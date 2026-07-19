import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/features/sync/data/models/pending_operation.dart';
import 'package:hardware_paint_shop_mobile/features/sync/data/repositories/sync_repository.dart';

class SyncState {
  const SyncState({
    this.status = ViewStatus.initial,
    this.pending = const [],
    this.lastSync,
    this.online = false,
    this.message,
  });

  final ViewStatus status;
  final List<PendingOperation> pending;
  final DateTime? lastSync;
  final bool online;
  final String? message;
}

class SyncCubit extends Cubit<SyncState> {
  SyncCubit(this._repository) : super(const SyncState());

  final SyncRepository _repository;

  String get baseUrl => _repository.baseUrl;

  Future<void> initialize() async {
    try {
      final snapshot = await _repository.localSnapshot();
      emit(
        SyncState(
          status: ViewStatus.success,
          pending: snapshot.pending,
          lastSync: snapshot.lastSync,
        ),
      );
    } catch (error) {
      emit(
        SyncState(
          status: ViewStatus.failure,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }

  Future<void> synchronize() async {
    emit(
      SyncState(
        status: ViewStatus.loading,
        pending: state.pending,
        lastSync: state.lastSync,
        online: state.online,
      ),
    );
    try {
      final snapshot = await _repository.synchronize(state.lastSync);
      emit(
        SyncState(
          status: ViewStatus.success,
          pending: snapshot.pending,
          lastSync: snapshot.lastSync,
          online: true,
          message: 'اكتملت المزامنة.',
        ),
      );
    } catch (error) {
      emit(
        SyncState(
          status: ViewStatus.failure,
          pending: state.pending,
          lastSync: state.lastSync,
          online: false,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }

  Future<void> refreshPending() async {
    try {
      final snapshot = await _repository.localSnapshot();
      emit(
        SyncState(
          status: ViewStatus.success,
          pending: snapshot.pending,
          lastSync: snapshot.lastSync,
          online: state.online,
        ),
      );
    } catch (error) {
      emit(
        SyncState(
          status: ViewStatus.failure,
          pending: state.pending,
          lastSync: state.lastSync,
          online: state.online,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }
}
