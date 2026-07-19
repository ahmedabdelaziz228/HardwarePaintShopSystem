import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/network/api_client.dart';
import 'package:hardware_paint_shop_mobile/features/auth/data/models/user_session.dart';

/// Authentication data boundary for API and persisted bearer-token access.
class AuthRepository {
  AuthRepository(this._apiClient);

  final ApiClient _apiClient;

  String get baseUrl => _apiClient.baseUrl;
  bool get hasStoredToken => _apiClient.hasToken;

  Future<void> initialize() async {
    try {
      await _apiClient.initialize();
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<UserSession> restoreSession() async {
    try {
      final json = await _apiClient.get<Map<String, dynamic>>('/api/auth/me');
      return UserSession.fromJson(json);
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<UserSession> login({
    required String apiUrl,
    required String username,
    required String password,
    required String deviceName,
  }) async {
    try {
      await _apiClient.configure(apiUrl);
      final json = await _apiClient.login(
        username.trim(),
        password,
        deviceName,
      );
      return UserSession.fromJson(json);
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<void> logout() async {
    try {
      await _apiClient.logout();
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }
}
