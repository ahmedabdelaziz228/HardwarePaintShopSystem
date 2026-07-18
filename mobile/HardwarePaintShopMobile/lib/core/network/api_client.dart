import 'dart:convert';
import 'dart:io';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:hardware_paint_shop_mobile/core/config/app_config.dart';
import 'package:hardware_paint_shop_mobile/core/errors/api_exception.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

/// Handles HTTP transport, API errors, URL preferences, and secure bearer tokens.
class ApiClient {
  ApiClient({http.Client? client}) : _client = client ?? http.Client();

  static const _secure = FlutterSecureStorage();
  final http.Client _client;
  String _baseUrl = '';
  String? _token;

  String get baseUrl => _baseUrl;
  bool get hasToken => _token?.isNotEmpty == true;

  Future<void> initialize() async {
    final prefs = await SharedPreferences.getInstance();
    _baseUrl = normalizeBaseUrl(
      prefs.getString('api_url') ?? AppConfig.defaultApiBaseUrl,
    );
    _token = await _secure.read(key: 'api_token');
  }

  Future<void> configure(String url) async {
    _baseUrl = normalizeBaseUrl(url);
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('api_url', _baseUrl);
  }

  Future<Map<String, dynamic>> login(String username, String password, String deviceName) async {
    final response = await _send('POST', '/api/auth/login', body: {
      'username': username,
      'password': password,
      'deviceName': deviceName,
    }, authenticated: false);
    _token = '${response['token']}';
    await _secure.write(key: 'api_token', value: _token);
    return response;
  }

  Future<void> logout() async {
    try {
      if (hasToken) await post('/api/auth/logout');
    } finally {
      _token = null;
      await _secure.delete(key: 'api_token');
    }
  }

  Future<dynamic> get(String path, {Map<String, String>? query}) =>
      _send('GET', path, query: query);
  Future<dynamic> post(String path, {Object? body}) => _send('POST', path, body: body);

  String imageUrl(String productId) => '$_baseUrl/api/products/$productId/image';
  Map<String, String> get imageHeaders => _token == null ? {} : {'Authorization': 'Bearer $_token'};

  Future<dynamic> _send(
    String method,
    String path, {
    Map<String, String>? query,
    Object? body,
    bool authenticated = true,
  }) async {
    if (_baseUrl.isEmpty) throw const ApiException('أدخل عنوان API أولًا.');
    var uri = Uri.parse('$_baseUrl$path');
    if (query != null) uri = uri.replace(queryParameters: query);
    final headers = <String, String>{'Accept': 'application/json', 'Content-Type': 'application/json'};
    if (authenticated && _token != null) headers['Authorization'] = 'Bearer $_token';
    try {
      final response = method == 'GET'
          ? await _client.get(uri, headers: headers).timeout(const Duration(seconds: 15))
          : await _client.post(uri, headers: headers, body: body == null ? null : jsonEncode(body))
              .timeout(const Duration(seconds: 25));
      dynamic decoded;
      if (response.body.isNotEmpty) {
        try {
          decoded = jsonDecode(utf8.decode(response.bodyBytes));
        } catch (_) {
          decoded = response.body;
        }
      }
      if (response.statusCode < 200 || response.statusCode >= 300) {
        final message = decoded is Map ? '${decoded['error'] ?? 'فشل الطلب'}' : 'فشل الطلب (${response.statusCode})';
        if (response.statusCode == 401) {
          _token = null;
          await _secure.delete(key: 'api_token');
        }
        throw ApiException(message, statusCode: response.statusCode);
      }
      return decoded;
    } on SocketException {
      throw const ApiException('تعذر الوصول إلى اللاب. تأكد من الواي فاي وعنوان API.');
    } on HttpException catch (e) {
      throw ApiException(e.message);
    }
  }

  static String normalizeBaseUrl(String value) {
    var url = value.trim();
    if (!url.startsWith('http://') && !url.startsWith('https://')) url = 'http://$url';
    while (url.endsWith('/')) {
      url = url.substring(0, url.length - 1);
    }
    return url;
  }
}
