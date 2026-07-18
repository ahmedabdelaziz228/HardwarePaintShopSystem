import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/utils/id_generator.dart';
import 'package:hardware_paint_shop_mobile/features/auth/data/models/user_session.dart';
import 'package:hardware_paint_shop_mobile/features/auth/data/repositories/auth_repository.dart';
import 'package:shared_preferences/shared_preferences.dart';

enum AuthStatus { initial, loading, authenticated, unauthenticated }

/// Immutable state consumed by authentication UI and permission checks.
class AuthState {
  const AuthState({
    this.status = AuthStatus.initial,
    this.baseUrl = '',
    this.userName = '',
    this.permissions = const {},
    this.errorMessage,
  });

  final AuthStatus status;
  final String baseUrl;
  final String userName;
  final Set<String> permissions;
  final String? errorMessage;

  bool get initialized => status != AuthStatus.initial;
  bool get authenticated => status == AuthStatus.authenticated;
  bool get busy => status == AuthStatus.loading;
  bool can(String permission) => permissions.contains(permission);

  AuthState copyWith({
    AuthStatus? status,
    String? baseUrl,
    String? userName,
    Set<String>? permissions,
    String? errorMessage,
    bool clearError = false,
  }) =>
      AuthState(
        status: status ?? this.status,
        baseUrl: baseUrl ?? this.baseUrl,
        userName: userName ?? this.userName,
        permissions: permissions ?? this.permissions,
        errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
      );
}

/// Coordinates login/session transitions; all failures become explicit states.
class AuthCubit extends Cubit<AuthState> {
  AuthCubit(this._repository) : super(const AuthState());

  final AuthRepository _repository;

  Future<void> initialize() async {
    emit(state.copyWith(status: AuthStatus.loading, clearError: true));
    try {
      await _repository.initialize();
      if (_repository.hasStoredToken) {
        final session = await _repository.restoreSession();
        _emitSession(session);
      } else {
        emit(
          state.copyWith(
            status: AuthStatus.unauthenticated,
            baseUrl: _repository.baseUrl,
            clearError: true,
          ),
        );
      }
    } catch (error) {
      emit(
        state.copyWith(
          status: AuthStatus.unauthenticated,
          baseUrl: _repository.baseUrl,
          errorMessage: ErrorMapper.message(error),
        ),
      );
    }
  }

  Future<void> login(
    String apiUrl,
    String username,
    String password,
  ) async {
    if (state.busy) return;
    emit(state.copyWith(status: AuthStatus.loading, clearError: true));
    try {
      final session = await _repository.login(
        apiUrl: apiUrl,
        username: username,
        password: password,
        deviceName: await _deviceName(),
      );
      _emitSession(session);
    } catch (error) {
      emit(
        state.copyWith(
          status: AuthStatus.unauthenticated,
          baseUrl: _repository.baseUrl,
          errorMessage: ErrorMapper.message(error),
        ),
      );
    }
  }

  Future<void> logout() async {
    emit(state.copyWith(status: AuthStatus.loading, clearError: true));
    try {
      await _repository.logout();
      emit(
        AuthState(
          status: AuthStatus.unauthenticated,
          baseUrl: _repository.baseUrl,
        ),
      );
    } catch (error) {
      emit(
        AuthState(
          status: AuthStatus.unauthenticated,
          baseUrl: _repository.baseUrl,
          errorMessage: ErrorMapper.message(error),
        ),
      );
    }
  }

  void _emitSession(UserSession session) {
    emit(
      AuthState(
        status: AuthStatus.authenticated,
        baseUrl: _repository.baseUrl,
        userName: session.userName,
        permissions: session.permissions,
      ),
    );
  }

  Future<String> _deviceName() async {
    try {
      final preferences = await SharedPreferences.getInstance();
      var value = preferences.getString('device_name');
      if (value == null) {
        value = 'Android-${IdGenerator.uuidV4().substring(0, 8)}';
        await preferences.setString('device_name', value);
      }
      return value;
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }
}
