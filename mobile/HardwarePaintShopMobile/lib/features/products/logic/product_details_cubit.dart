import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/models/product_details.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/repositories/products_repository.dart';

class ProductDetailsState {
  const ProductDetailsState({
    this.status = ViewStatus.initial,
    this.details,
    this.imageUrl,
    this.imageHeaders = const {},
    this.message,
    this.adjustmentSaved = false,
  });

  final ViewStatus status;
  final ProductDetails? details;
  final String? imageUrl;
  final Map<String, String> imageHeaders;
  final String? message;
  final bool adjustmentSaved;
}

class ProductDetailsCubit extends Cubit<ProductDetailsState> {
  ProductDetailsCubit(this._repository) : super(const ProductDetailsState());

  final ProductsRepository _repository;
  String? _productId;

  Future<void> load(String productId) async {
    _productId = productId;
    emit(
      ProductDetailsState(
        status: ViewStatus.loading,
        details: state.details,
      ),
    );
    try {
      final details = await _repository.details(productId);
      emit(
        ProductDetailsState(
          status: ViewStatus.success,
          details: details,
          imageUrl: _repository.imageUrl(productId),
          imageHeaders: _repository.imageHeaders,
        ),
      );
    } catch (error) {
      emit(
        ProductDetailsState(
          status: ViewStatus.failure,
          details: state.details,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }

  Future<void> adjustStock(double difference, String reason) async {
    if (_productId == null) return;
    emit(
      ProductDetailsState(
        status: ViewStatus.loading,
        details: state.details,
        imageUrl: state.imageUrl,
        imageHeaders: state.imageHeaders,
      ),
    );
    try {
      final result = await _repository.adjustStock(
        _productId!,
        difference,
        reason,
      );
      final details = result.online
          ? await _repository.details(_productId!)
          : state.details;
      emit(
        ProductDetailsState(
          status: ViewStatus.success,
          details: details,
          imageUrl: _repository.imageUrl(_productId!),
          imageHeaders: _repository.imageHeaders,
          adjustmentSaved: true,
          message: result.online
              ? 'تم حفظ حركة المخزون.'
              : 'تم حفظ الحركة للمزامنة عند الاتصال.',
        ),
      );
    } catch (error) {
      emit(
        ProductDetailsState(
          status: ViewStatus.failure,
          details: state.details,
          imageUrl: state.imageUrl,
          imageHeaders: state.imageHeaders,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }
}
