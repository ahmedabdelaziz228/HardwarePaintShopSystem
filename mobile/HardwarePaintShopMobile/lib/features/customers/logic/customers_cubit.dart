import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/features/customers/data/models/customer_summary.dart';
import 'package:hardware_paint_shop_mobile/features/customers/data/repositories/customers_repository.dart';

class CustomersState {
  const CustomersState({
    this.status = ViewStatus.initial,
    this.customers = const [],
    this.query = '',
    this.online = false,
    this.message,
  });

  final ViewStatus status;
  final List<CustomerSummary> customers;
  final String query;
  final bool online;
  final String? message;
}

class CustomersCubit extends Cubit<CustomersState> {
  CustomersCubit(this._repository) : super(const CustomersState());

  final CustomersRepository _repository;

  Future<void> search(String query) async {
    emit(
      CustomersState(
        status: ViewStatus.loading,
        customers: state.customers,
        query: query,
        online: state.online,
      ),
    );
    try {
      final result = await _repository.search(query);
      emit(
        CustomersState(
          status: ViewStatus.success,
          customers: result.customers,
          query: query,
          online: result.online,
          message: result.warning,
        ),
      );
    } catch (error) {
      emit(
        CustomersState(
          status: ViewStatus.failure,
          customers: state.customers,
          query: query,
          online: false,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }
}
