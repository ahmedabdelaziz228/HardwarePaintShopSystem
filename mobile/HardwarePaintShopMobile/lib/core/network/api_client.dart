import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:hardware_paint_shop_mobile/core/config/app_config.dart';
import 'package:hardware_paint_shop_mobile/core/errors/api_exception.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Central Dio transport. Widgets and Cubits never perform HTTP calls directly.
class ApiClient {
  ApiClient({Dio? dio, FlutterSecureStorage? secureStorage})
      : _dio = dio ??
            Dio(
              BaseOptions(
                connectTimeout: const Duration(seconds: 15),
                sendTimeout: const Duration(seconds: 25),
                receiveTimeout: const Duration(seconds: 25),
                responseType: ResponseType.json,
                headers: const <String, dynamic>{
                  Headers.acceptHeader: Headers.jsonContentType,
                  Headers.contentTypeHeader: Headers.jsonContentType,
                },
                validateStatus: (status) => status != null,
              ),
            ),
        _secureStorage = secureStorage ?? const FlutterSecureStorage();

  static const _apiUrlKey = 'api_url';
  static const _tokenKey = 'api_token';

  final Dio _dio;
  final FlutterSecureStorage _secureStorage;
  String _baseUrl = '';
  String? _token;

  String get baseUrl => _baseUrl;
  bool get hasToken => _token?.isNotEmpty == true;
  String imageUrl(String productId) => '$_baseUrl/api/products/$productId/image';
  Map<String, String> get imageHeaders =>
      _token == null ? const {} : {'Authorization': 'Bearer $_token'};

  Future<void> initialize() async {
    try {
      final preferences = await SharedPreferences.getInstance();
      _baseUrl = normalizeBaseUrl(
        preferences.getString(_apiUrlKey) ?? AppConfig.defaultApiBaseUrl,
      );
      _token = await _secureStorage.read(key: _tokenKey);
    } catch (error) {
      throw StorageException(
        'تعذر قراءة إعدادات الاتصال المحفوظة.',
        cause: error,
      );
    }
  }

  Future<void> configure(String url) async {
    try {
      _baseUrl = normalizeBaseUrl(url);
      if (_baseUrl.isEmpty) {
        throw const ApiException('أدخل عنوان API أولًا.');
      }
      final preferences = await SharedPreferences.getInstance();
      await preferences.setString(_apiUrlKey, _baseUrl);
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<Map<String, dynamic>> login(
    String username,
    String password,
    String deviceName,
  ) async {
    try {
      final response = await _request<Map<String, dynamic>>(
        'POST',
        '/api/auth/login',
        body: {
          'username': username,
          'password': password,
          'deviceName': deviceName,
        },
        authenticated: false,
      );
      _token = '${response['token']}';
      await _secureStorage.write(key: _tokenKey, value: _token);
      return response;
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<void> logout() async {
    Object? remoteError;
    try {
      if (hasToken) await post('/api/auth/logout');
    } catch (error) {
      // A remote logout failure must not keep a local session alive.
      remoteError = error;
    } finally {
      _token = null;
      try {
        await _secureStorage.delete(key: _tokenKey);
      } catch (storageError) {
        throw StorageException(
          'تعذر حذف جلسة الدخول من الهاتف.',
          cause: storageError,
        );
      }
    }
    if (remoteError != null && remoteError is! NetworkException) {
      throw ErrorMapper.map(remoteError);
    }
  }

  Future<T> get<T>(
    String path, {
    Map<String, dynamic>? query,
  }) async {
    try {
      return await _request<T>('GET', path, query: query);
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<T> post<T>(String path, {Object? body}) async {
    try {
      return await _request<T>('POST', path, body: body);
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<T> _request<T>(
    String method,
    String path, {
    Map<String, dynamic>? query,
    Object? body,
    bool authenticated = true,
  }) async {
    if (_baseUrl.isEmpty) throw const ApiException('أدخل عنوان API أولًا.');
    try {
      final response = await _dio.request<dynamic>(
        '$_baseUrl$path',
        data: body,
        queryParameters: query,
        options: Options(
          method: method,
          headers: authenticated && _token != null
              ? {'Authorization': 'Bearer $_token'}
              : null,
        ),
      );
      final statusCode = response.statusCode ?? 0;
      final data = response.data;
      if (statusCode < 200 || statusCode >= 300) {
        if (statusCode == 401) {
          _token = null;
          await _secureStorage.delete(key: _tokenKey);
        }
        final message = data is Map
            ? '${data['error'] ?? data['message'] ?? 'فشل الطلب'}'
            : 'فشل الطلب ($statusCode)';
        throw ApiException(message, statusCode: statusCode);
      }
      return data as T;
    } on DioException catch (error) {
      throw _mapDioException(error);
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  static AppException _mapDioException(DioException error) {
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.transformTimeout:
      case DioExceptionType.connectionError:
        return NetworkException(
          'تعذر الوصول إلى اللاب. تأكد من الواي فاي وعنوان API.',
          cause: error,
        );
      case DioExceptionType.cancel:
        return NetworkException('تم إلغاء الطلب.', cause: error);
      case DioExceptionType.badCertificate:
        return NetworkException('شهادة اتصال API غير موثوقة.', cause: error);
      case DioExceptionType.badResponse:
        final statusCode = error.response?.statusCode;
        final data = error.response?.data;
        final message = data is Map
            ? '${data['error'] ?? data['message'] ?? 'فشل الطلب'}'
            : 'فشل الطلب (${statusCode ?? '-'})';
        return ApiException(message, statusCode: statusCode, cause: error);
      case DioExceptionType.unknown:
        return NetworkException('حدث خطأ في الاتصال بالخادم.', cause: error);
    }
  }

  static String normalizeBaseUrl(String value) {
    var url = value.trim();
    if (url.isEmpty) return '';
    if (!url.startsWith('http://') && !url.startsWith('https://')) {
      url = 'http://$url';
    }
    while (url.endsWith('/')) {
      url = url.substring(0, url.length - 1);
    }
    return url;
  }
}
