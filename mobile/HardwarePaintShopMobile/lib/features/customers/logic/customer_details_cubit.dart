import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/features/customers/data/models/cashbox_option.dart';
import 'package:hardware_paint_shop_mobile/features/customers/data/models/customer_statement.dart';
import 'package:hardware_paint_shop_mobile/features/customers/data/repositories/customers_repository.dart';

class CustomerDetailsState {
  const CustomerDetailsState({
    this.status = ViewStatus.initial,
    this.statement,
    this.cashboxes = const [],
    this.message,
    this.collectionSaved = false,
  });

  final ViewStatus status;
  final CustomerStatement? statement;
  final List<CashboxOption> cashboxes;
  final String? message;
  final bool collectionSaved;
}

class CustomerDetailsCubit extends Cubit<CustomerDetailsState> {
  CustomerDetailsCubit(this._repository) : super(const CustomerDetailsState());

  final CustomersRepository _repository;
  String? _customerId;

  Future<void> load(String customerId) async {
    _customerId = customerId;
    emit(
      CustomerDetailsState(
        status: ViewStatus.loading,
        statement: state.statement,
        cashboxes: state.cashboxes,
      ),
    );
    try {
      final values = await Future.wait<dynamic>([
        _repository.statement(customerId),
        _repository.cashboxes(),
      ]);
      emit(
        CustomerDetailsState(
          status: ViewStatus.success,
          statement: values[0] as CustomerStatement,
          cashboxes: values[1] as List<CashboxOption>,
        ),
      );
    } catch (error) {
      emit(
        CustomerDetailsState(
          status: ViewStatus.failure,
          statement: state.statement,
          cashboxes: state.cashboxes,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }

  Future<void> collect({
    required double amount,
    required String cashboxId,
    required String notes,
  }) async {
    if (_customerId == null) return;
    emit(
      CustomerDetailsState(
        status: ViewStatus.loading,
        statement: state.statement,
        cashboxes: state.cashboxes,
      ),
    );
    try {
      final online = await _repository.collect(
        customerId: _customerId!,
        amount: amount,
        cashboxId: cashboxId,
        notes: notes,
      );
      final statement = online
          ? await _repository.statement(_customerId!)
          : state.statement;
      emit(
        CustomerDetailsState(
          status: ViewStatus.success,
          statement: statement,
          cashboxes: state.cashboxes,
          collectionSaved: true,
          message: online
              ? 'تم حفظ التحصيل.'
              : 'تم حفظ التحصيل للمزامنة عند الاتصال.',
        ),
      );
    } catch (error) {
      emit(
        CustomerDetailsState(
          status: ViewStatus.failure,
          statement: state.statement,
          cashboxes: state.cashboxes,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }
}
