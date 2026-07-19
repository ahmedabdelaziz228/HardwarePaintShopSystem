import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/features/alerts/data/models/shop_alert.dart';
import 'package:hardware_paint_shop_mobile/features/alerts/data/repositories/alerts_repository.dart';

class AlertsState {
  const AlertsState({
    this.status = ViewStatus.initial,
    this.alerts = const [],
    this.online = false,
    this.message,
  });

  final ViewStatus status;
  final List<ShopAlert> alerts;
  final bool online;
  final String? message;
}

class AlertsCubit extends Cubit<AlertsState> {
  AlertsCubit(this._repository) : super(const AlertsState());

  final AlertsRepository _repository;

  Future<void> load() async {
    emit(
      AlertsState(
        status: ViewStatus.loading,
        alerts: state.alerts,
        online: state.online,
      ),
    );
    try {
      final result = await _repository.load();
      emit(
        AlertsState(
          status: ViewStatus.success,
          alerts: result.alerts,
          online: result.online,
          message: result.warning,
        ),
      );
    } catch (error) {
      emit(
        AlertsState(
          status: ViewStatus.failure,
          alerts: state.alerts,
          online: false,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }

  Future<void> markRead(String alertId) async {
    try {
      await _repository.markRead(alertId);
      await load();
    } catch (error) {
      emit(
        AlertsState(
          status: ViewStatus.failure,
          alerts: state.alerts,
          online: state.online,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }
}
