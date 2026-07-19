import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/core/utils/formatters.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/app_message.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/error_banner.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/info_card.dart';
import 'package:hardware_paint_shop_mobile/features/auth/logic/auth_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/models/product_summary.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/repositories/products_repository.dart';
import 'package:hardware_paint_shop_mobile/features/products/logic/product_details_cubit.dart';

class ProductDetailsPage extends StatelessWidget {
  const ProductDetailsPage({super.key, required this.product});

  final ProductSummary product;

  @override
  Widget build(BuildContext context) => BlocProvider(
        create: (context) => ProductDetailsCubit(
          context.read<ProductsRepository>(),
        )..load(product.id),
        child: _ProductDetailsView(product: product),
      );
}

class _ProductDetailsView extends StatelessWidget {
  const _ProductDetailsView({required this.product});

  final ProductSummary product;

  @override
  Widget build(BuildContext context) {
    final canAdjust = context.select(
      (AuthCubit cubit) => cubit.state.can('Inventory.Adjust'),
    );
    return BlocConsumer<ProductDetailsCubit, ProductDetailsState>(
      listenWhen: (previous, current) =>
          previous.message != current.message && current.message != null,
      listener: (context, state) => showAppMessage(
        context,
        state.message!,
        isError: state.status == ViewStatus.failure,
      ),
      builder: (context, state) {
        final details = state.details;
        return Scaffold(
          appBar: AppBar(title: Text(product.name)),
          floatingActionButton: canAdjust && details != null
              ? FloatingActionButton.extended(
                  onPressed: () => _showStockAdjustment(context, product),
                  icon: const Icon(Icons.tune),
                  label: const Text('تسوية'),
                )
              : null,
          body: details == null
              ? Center(
                  child: state.status == ViewStatus.failure
                      ? ErrorBanner(state.message ?? 'تعذر تحميل المنتج.')
                      : const CircularProgressIndicator(),
                )
              : RefreshIndicator(
                  onRefresh: () =>
                      context.read<ProductDetailsCubit>().load(product.id),
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      if (details.hasImage && state.imageUrl != null)
                        ClipRRect(
                          borderRadius: BorderRadius.circular(18),
                          child: Image.network(
                            state.imageUrl!,
                            headers: state.imageHeaders,
                            height: 210,
                            fit: BoxFit.cover,
                            semanticLabel: 'صورة ${details.name}',
                            errorBuilder: (_, __, ___) =>
                                const SizedBox.shrink(),
                          ),
                        ),
                      const SizedBox(height: 12),
                      InfoCard(
                        children: [
                          InfoRow(label: 'الكود', value: details.code ?? '—'),
                          InfoRow(
                            label: 'التصنيف',
                            value: details.category ?? '—',
                          ),
                          InfoRow(
                            label: 'المورد',
                            value: details.mainSupplier ?? '—',
                          ),
                          InfoRow(
                            label: 'الرصيد الأساسي',
                            value:
                                '${details.stockBase.toStringAsFixed(3)} ${details.baseUnit ?? ''}',
                          ),
                          InfoRow(
                            label: 'السيريالات المتاحة',
                            value: '${details.availableSerials}',
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      Text(
                        'الأسعار',
                        style: Theme.of(context)
                            .textTheme
                            .titleMedium
                            ?.copyWith(fontWeight: FontWeight.w800),
                      ),
                      const SizedBox(height: 8),
                      ...details.prices.map(
                        (price) => Card(
                          child: ListTile(
                            title: Text(price.priceGroup),
                            subtitle: Text(
                              'أقل سعر ${money(price.minimumSalePrice)}',
                            ),
                            trailing: Text(
                              money(price.salePrice),
                              style: const TextStyle(fontWeight: FontWeight.w800),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
        );
      },
    );
  }

  Future<void> _showStockAdjustment(
    BuildContext context,
    ProductSummary product,
  ) async {
    final differenceController = TextEditingController();
    final reasonController = TextEditingController();
    try {
      await showDialog<void>(
        context: context,
        builder: (dialogContext) => AlertDialog(
          title: Text('تسوية ${product.name}'),
          content: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Text('الرصيد الحالي: ${product.stockBase.toStringAsFixed(3)}'),
              const SizedBox(height: 12),
              TextField(
                controller: differenceController,
                keyboardType: const TextInputType.numberWithOptions(
                  signed: true,
                  decimal: true,
                ),
                decoration: const InputDecoration(
                  labelText: 'الفرق (+ زيادة / - نقص)',
                ),
              ),
              const SizedBox(height: 10),
              TextField(
                controller: reasonController,
                decoration: const InputDecoration(labelText: 'السبب *'),
              ),
            ],
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(dialogContext),
              child: const Text('إلغاء'),
            ),
            FilledButton(
              onPressed: () {
                final difference =
                    double.tryParse(differenceController.text) ?? 0;
                context.read<ProductDetailsCubit>().adjustStock(
                      difference,
                      reasonController.text,
                    );
                Navigator.pop(dialogContext);
              },
              child: const Text('حفظ'),
            ),
          ],
        ),
      );
    } catch (error) {
      if (context.mounted) showAppMessage(context, '$error', isError: true);
    } finally {
      differenceController.dispose();
      reasonController.dispose();
    }
  }
}
