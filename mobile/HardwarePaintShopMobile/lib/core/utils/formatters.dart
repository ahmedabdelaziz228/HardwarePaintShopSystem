import 'package:hardware_paint_shop_mobile/core/utils/json_parsing.dart';
import 'package:intl/intl.dart' as intl;

/// Formats money consistently across all feature UIs.
String money(dynamic value) => intl.NumberFormat.currency(
      locale: 'ar_EG',
      symbol: 'ج.م',
      decimalDigits: 2,
    ).format(asDouble(value));
