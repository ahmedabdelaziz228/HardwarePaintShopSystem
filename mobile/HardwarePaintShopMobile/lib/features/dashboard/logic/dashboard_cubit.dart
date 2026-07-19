import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/features/dashboard/data/models/dashboard_summary.dart';
import 'package:hardware_paint_shop_mobile/features/dashboard/data/repositories/dashboard_repository.dart';

class DashboardState {
  const DashboardState({
    this.status = ViewStatus.initial,
    this.summary = const DashboardSummary(),
    this.shopName = 'محل الحدايد والبوهيات',
    this.online = false,
    this.errorMessage,
  });

  final ViewStatus status;
  final DashboardSummary summary;
  final String shopName;
  final bool online;
  final String? errorMessage;

  DashboardState copyWith({
    ViewStatus? status,
    DashboardSummary? summary,
    String? shopName,
    bool? online,
    String? errorMessage,
    bool clearError = false,
  }) =>
      DashboardState(
        status: status ?? this.status,
        summary: summary ?? this.summary,
        shopName: shopName ?? this.shopName,
        online: online ?? this.online,
        errorMessage: clearError ? null : errorMessage ?? this.errorMessage,
      );
}

class DashboardCubit extends Cubit<DashboardState> {
  DashboardCubit(this._repository) : super(const DashboardState());

  final DashboardRepository _repository;

  Future<void> load({bool showLoader = true}) async {
    if (showLoader) {
      emit(state.copyWith(status: ViewStatus.loading, clearError: true));
    }
    try {
      final payload = await _repository.load();
      emit(
        DashboardState(
          status: ViewStatus.success,
          summary: payload.summary,
          shopName: payload.shopName,
          online: true,
        ),
      );
    } catch (error) {
      emit(
        state.copyWith(
          status: ViewStatus.failure,
          online: false,
          errorMessage: ErrorMapper.message(error),
        ),
      );
    }
  }
}
