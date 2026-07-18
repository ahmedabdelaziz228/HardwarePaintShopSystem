import 'package:hardware_paint_shop_mobile/core/utils/json_parsing.dart';

/// Lightweight product row used by search, cache, and inventory screens.
class ProductSummary {
  const ProductSummary({
    required this.id,
    required this.name,
    required this.stockBase,
    this.code,
    this.category,
    this.baseUnit,
    this.defaultPrice,
    this.minimum = 0,
    this.serialTracked = false,
    this.hasImage = false,
    this.updatedAt,
  });

  final String id;
  final String name;
  final String? code;
  final String? category;
  final String? baseUnit;
  final double stockBase;
  final double? defaultPrice;
  final double minimum;
  final bool serialTracked;
  final bool hasImage;
  final DateTime? updatedAt;

  bool get isLow => stockBase <= minimum;

  factory ProductSummary.fromJson(Map<String, dynamic> json) => ProductSummary(
        id: '${json['id']}',
        name: '${json['name'] ?? ''}',
        code: json['productCode']?.toString(),
        category: json['category']?.toString(),
        baseUnit: json['baseUnit']?.toString(),
        stockBase: asDouble(json['stockBase']),
        defaultPrice: json['defaultPrice'] == null
            ? null
            : asDouble(json['defaultPrice']),
        minimum: asDouble(json['minStockBaseQuantity']),
        serialTracked: json['isSerialTracked'] == true,
        hasImage: json['hasImage'] == true,
        updatedAt: DateTime.tryParse('${json['updatedAt'] ?? ''}'),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'productCode': code,
        'category': category,
        'baseUnit': baseUnit,
        'stockBase': stockBase,
        'defaultPrice': defaultPrice,
        'minStockBaseQuantity': minimum,
        'isSerialTracked': serialTracked,
        'hasImage': hasImage,
        'updatedAt': updatedAt?.toIso8601String(),
      };
}
