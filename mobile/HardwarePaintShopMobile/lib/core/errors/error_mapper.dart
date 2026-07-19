import 'package:hardware_paint_shop_mobile/core/errors/api_exception.dart';

/// Converts unknown infrastructure failures to stable Arabic messages for UI state.
abstract final class ErrorMapper {
  static AppException map(Object error) {
    if (error is AppException) return error;
    return AppException('حدث خطأ غير متوقع. حاول مرة أخرى.', cause: error);
  }

  static String message(Object error) => map(error).message;
}
