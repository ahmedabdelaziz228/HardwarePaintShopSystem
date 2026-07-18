import 'package:hardware_paint_shop_mobile/core/network/api_client.dart';
import 'package:hardware_paint_shop_mobile/features/auth/data/user_session.dart';

/// Owns authentication API calls and session token persistence through [ApiClient].
class AuthRepository {
  AuthRepository(this._apiClient);

  final ApiClient _apiClient;

  String get baseUrl => _apiClient.baseUrl;
  bool get hasStoredToken => _apiClient.hasToken;

  Future<void> initialize() => _apiClient.initialize();

  Future<UserSession> restoreSession() async {
    final json = await _apiClient.get('/api/auth/me') as Map<String, dynamic>;
    return UserSession.fromJson(json);
  }

  Future<UserSession> login({
    required String apiUrl,
    required String username,
    required String password,
    required String deviceName,
  }) async {
    await _apiClient.configure(apiUrl);
    final json = await _apiClient.login(username.trim(), password, deviceName);
    return UserSession.fromJson(json);
  }

  Future<void> logout() => _apiClient.logout();
}
