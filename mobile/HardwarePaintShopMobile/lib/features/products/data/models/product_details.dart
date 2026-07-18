import 'package:hardware_paint_shop_mobile/core/utils/json_parsing.dart';

class ProductPrice {
  const ProductPrice({
    required this.priceGroup,
    required this.salePrice,
    required this.minimumSalePrice,
  });

  final String priceGroup;
  final double salePrice;
  final double minimumSalePrice;

  factory ProductPrice.fromJson(Map<String, dynamic> json) => ProductPrice(
        priceGroup: '${json['priceGroup'] ?? '—'}',
        salePrice: asDouble(json['salePrice']),
        minimumSalePrice: asDouble(json['minSalePrice']),
      );
}

/// Full product payload used by the details page.
class ProductDetails {
  const ProductDetails({
    required this.id,
    required this.name,
    required this.stockBase,
    required this.availableSerials,
    required this.prices,
    this.code,
    this.category,
    this.mainSupplier,
    this.baseUnit,
    this.hasImage = false,
  });

  final String id;
  final String name;
  final String? code;
  final String? category;
  final String? mainSupplier;
  final String? baseUnit;
  final double stockBase;
  final int availableSerials;
  final bool hasImage;
  final List<ProductPrice> prices;

  factory ProductDetails.fromJson(Map<String, dynamic> json) => ProductDetails(
        id: '${json['id']}',
        name: '${json['name'] ?? ''}',
        code: json['productCode']?.toString(),
        category: json['category']?.toString(),
        mainSupplier: json['mainSupplier']?.toString(),
        baseUnit: json['baseUnit']?.toString(),
        stockBase: asDouble(json['stockBase']),
        availableSerials:
            int.tryParse('${json['availableSerials'] ?? 0}') ?? 0,
        hasImage: json['hasImage'] == true,
        prices: ((json['prices'] as List?) ?? const [])
            .map((row) => ProductPrice.fromJson(
                  Map<String, dynamic>.from(row as Map),
                ))
            .toList(),
      );
}
