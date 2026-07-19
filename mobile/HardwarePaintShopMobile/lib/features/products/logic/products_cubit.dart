import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/models/product_summary.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/repositories/products_repository.dart';

class ProductsState {
  const ProductsState({
    this.status = ViewStatus.initial,
    this.products = const [],
    this.query = '',
    this.online = false,
    this.message,
  });

  final ViewStatus status;
  final List<ProductSummary> products;
  final String query;
  final bool online;
  final String? message;
}

class ProductsCubit extends Cubit<ProductsState> {
  ProductsCubit(this._repository) : super(const ProductsState());

  final ProductsRepository _repository;

  Future<void> search(String query) async {
    emit(
      ProductsState(
        status: ViewStatus.loading,
        products: state.products,
        query: query,
        online: state.online,
      ),
    );
    try {
      final result = await _repository.search(query);
      emit(
        ProductsState(
          status: ViewStatus.success,
          products: result.products,
          query: query,
          online: result.online,
          message: result.warning,
        ),
      );
    } catch (error) {
      emit(
        ProductsState(
          status: ViewStatus.failure,
          products: state.products,
          query: query,
          online: false,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }

  Future<void> scan(String value, {bool serial = false}) async {
    emit(
      ProductsState(
        status: ViewStatus.loading,
        products: state.products,
        query: value,
        online: state.online,
      ),
    );
    try {
      final product = await _repository.scan(value, serial: serial);
      emit(
        ProductsState(
          status: ViewStatus.success,
          products: [product],
          query: value,
          online: true,
        ),
      );
    } catch (error) {
      emit(
        ProductsState(
          status: ViewStatus.failure,
          products: state.products,
          query: value,
          online: false,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }
}
