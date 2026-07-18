import 'package:flutter/material.dart';
import 'package:hardware_paint_shop_mobile/core/utils/formatters.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/models/product_summary.dart';

class ProductTile extends StatelessWidget {
  const ProductTile({
    super.key,
    required this.product,
    required this.onTap,
  });

  final ProductSummary product;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Card(
        child: InkWell(
          borderRadius: BorderRadius.circular(12),
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.all(13),
            child: Row(
              children: [
                Container(
                  width: 48,
                  height: 48,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: const Color(0xffeff6ff),
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: const Icon(
                    Icons.inventory_2_outlined,
                    color: Color(0xff2563eb),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        product.name,
                        style: const TextStyle(fontWeight: FontWeight.w700),
                      ),
                      const SizedBox(height: 3),
                      Text(
                        [product.code, product.category]
                            .whereType<String>()
                            .join(' • '),
                        style: const TextStyle(
                          fontSize: 12,
                          color: Color(0xff64748b),
                        ),
                      ),
                    ],
                  ),
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      product.defaultPrice == null
                          ? 'بدون سعر'
                          : money(product.defaultPrice),
                      style: const TextStyle(
                        fontWeight: FontWeight.w800,
                        color: Color(0xff2563eb),
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${product.stockBase.toStringAsFixed(3)} ${product.baseUnit ?? ''}',
                      style: TextStyle(
                        fontSize: 12,
                        color: product.isLow
                            ? const Color(0xffdc2626)
                            : const Color(0xff059669),
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      );
}
