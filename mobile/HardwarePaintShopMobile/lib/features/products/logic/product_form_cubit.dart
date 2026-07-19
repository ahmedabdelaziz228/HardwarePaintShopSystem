import 'dart:convert';
import 'dart:io';

import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/models/product_lookups.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/repositories/products_repository.dart';

class ProductFormState {
  const ProductFormState({
    this.status = ViewStatus.initial,
    this.lookups = const ProductLookups(),
    this.saved = false,
    this.online = false,
    this.message,
  });

  final ViewStatus status;
  final ProductLookups lookups;
  final bool saved;
  final bool online;
  final String? message;
}

class ProductFormCubit extends Cubit<ProductFormState> {
  ProductFormCubit(this._repository) : super(const ProductFormState());

  final ProductsRepository _repository;

  Future<void> loadLookups() async {
    emit(
      ProductFormState(
        status: ViewStatus.loading,
        lookups: state.lookups,
      ),
    );
    try {
      final lookups = await _repository.loadLookups();
      emit(ProductFormState(status: ViewStatus.success, lookups: lookups));
    } catch (error) {
      emit(
        ProductFormState(
          status: ViewStatus.failure,
          lookups: state.lookups,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }

  Future<void> create({
    required String name,
    required String code,
    required String barcode,
    required String baseUnitId,
    required String? categoryId,
    required String? priceGroupId,
    required double? salePrice,
    required double minimumStock,
    required bool serialTracked,
    String? imagePath,
  }) async {
    emit(
      ProductFormState(
        status: ViewStatus.loading,
        lookups: state.lookups,
      ),
    );
    try {
      String? imageBase64;
      if (imagePath != null) {
        imageBase64 = base64Encode(await File(imagePath).readAsBytes());
      }
      final result = await _repository.create({
        'name': name,
        'productCode': code,
        'baseUnitId': baseUnitId,
        'categoryId': categoryId,
        'mainSupplierId': null,
        'minStockBaseQuantity': minimumStock,
        'isSerialTracked': serialTracked,
        'barcode': barcode,
        'priceGroupId': priceGroupId,
        'salePrice': salePrice,
        'minSalePrice': 0,
        'imageBase64': imageBase64,
        'notes': 'أضيف من الموبايل',
      });
      emit(
        ProductFormState(
          status: ViewStatus.success,
          lookups: state.lookups,
          saved: true,
          online: result.online,
          message: result.online
              ? 'تم حفظ المنتج.'
              : 'تم حفظ المنتج للمزامنة عند الاتصال.',
        ),
      );
    } catch (error) {
      emit(
        ProductFormState(
          status: ViewStatus.failure,
          lookups: state.lookups,
          message: ErrorMapper.message(error),
        ),
      );
    }
  }
}
