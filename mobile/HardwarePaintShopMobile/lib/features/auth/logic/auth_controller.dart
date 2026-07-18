import 'package:flutter/foundation.dart';
import 'package:hardware_paint_shop_mobile/core/utils/id_generator.dart';
import 'package:hardware_paint_shop_mobile/features/auth/data/auth_repository.dart';
import 'package:hardware_paint_shop_mobile/features/auth/data/user_session.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Coordinates authentication UI state without leaking API calls into widgets.
class AuthController extends ChangeNotifier {
  AuthController(this._repository);

  final AuthRepository _repository;

  bool initialized = false;
  bool authenticated = false;
  bool busy = false;
  String? error;
  String userName = '';
  Set<String> permissions = {};

  String get baseUrl => _repository.baseUrl;
  bool can(String permission) => permissions.contains(permission);

  Future<void> initialize() async {
    try {
      await _repository.initialize();
      if (_repository.hasStoredToken) {
        _applySession(await _repository.restoreSession());
        authenticated = true;
      }
    } catch (exception) {
      error = '$exception';
      authenticated = false;
    } finally {
      initialized = true;
      notifyListeners();
    }
  }

  Future<void> login(String apiUrl, String username, String password) async {
    busy = true;
    error = null;
    notifyListeners();
    try {
      final session = await _repository.login(
        apiUrl: apiUrl,
        username: username,
        password: password,
        deviceName: await _deviceName(),
      );
      _applySession(session);
      authenticated = true;
    } catch (exception) {
      error = '$exception';
      rethrow;
    } finally {
      busy = false;
      notifyListeners();
    }
  }

  Future<void> logout() async {
    try {
      await _repository.logout();
    } finally {
      authenticated = false;
      userName = '';
      permissions = {};
      notifyListeners();
    }
  }

  void _applySession(UserSession session) {
    userName = session.userName;
    permissions = session.permissions;
  }

  Future<String> _deviceName() async {
    final preferences = await SharedPreferences.getInstance();
    var value = preferences.getString('device_name');
    if (value == null) {
      value = 'Android-${IdGenerator.uuidV4().substring(0, 8)}';
      await preferences.setString('device_name', value);
    }
    return value;
  }

}
