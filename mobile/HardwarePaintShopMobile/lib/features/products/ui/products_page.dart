import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/app_message.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/error_banner.dart';
import 'package:hardware_paint_shop_mobile/features/auth/logic/auth_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/products/logic/products_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/products/ui/add_product_page.dart';
import 'package:hardware_paint_shop_mobile/features/products/ui/product_details_page.dart';
import 'package:hardware_paint_shop_mobile/features/products/ui/scanner_page.dart';
import 'package:hardware_paint_shop_mobile/features/products/ui/widgets/product_tile.dart';

class ProductsPage extends StatefulWidget {
  const ProductsPage({super.key});

  @override
  State<ProductsPage> createState() => _ProductsPageState();
}

class _ProductsPageState extends State<ProductsPage> {
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    if (context.read<ProductsCubit>().state.status == ViewStatus.initial) {
      context.read<ProductsCubit>().search('');
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final canCreate = context.select(
      (AuthCubit cubit) => cubit.state.can('Product.Create'),
    );
    return BlocBuilder<ProductsCubit, ProductsState>(
      builder: (context, state) => Scaffold(
        backgroundColor: Colors.transparent,
        floatingActionButton: canCreate
            ? FloatingActionButton.extended(
                onPressed: () async {
                  final productsCubit = context.read<ProductsCubit>();
                  await Navigator.push(
                    context,
                    MaterialPageRoute<void>(
                      builder: (_) => const AddProductPage(),
                    ),
                  );
                  if (!mounted) return;
                  await productsCubit.search(_searchController.text);
                },
                icon: const Icon(Icons.add),
                label: const Text('منتج'),
              )
            : null,
        body: Column(
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: TextField(
                controller: _searchController,
                onSubmitted: context.read<ProductsCubit>().search,
                decoration: InputDecoration(
                  hintText: 'اسم، كود، باركود أو سيريال',
                  prefixIcon: const Icon(Icons.search),
                  suffixIcon: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      IconButton(
                        tooltip: 'بحث',
                        onPressed: () => context
                            .read<ProductsCubit>()
                            .search(_searchController.text),
                        icon: const Icon(Icons.arrow_forward),
                      ),
                      IconButton(
                        tooltip: 'مسح باركود',
                        onPressed: _scan,
                        icon: const Icon(Icons.qr_code_scanner),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            if (state.message != null)
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: ErrorBanner(state.message!),
              ),
            if (state.status == ViewStatus.loading && state.products.isNotEmpty)
              const LinearProgressIndicator(),
            Expanded(
              child: state.status == ViewStatus.loading && state.products.isEmpty
                  ? const Center(child: CircularProgressIndicator())
                  : ListView.separated(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 100),
                      itemCount: state.products.length,
                      separatorBuilder: (_, __) => const SizedBox(height: 9),
                      itemBuilder: (_, index) {
                        final product = state.products[index];
                        return ProductTile(
                          product: product,
                          onTap: () => Navigator.push(
                            context,
                            MaterialPageRoute<void>(
                              builder: (_) => ProductDetailsPage(product: product),
                            ),
                          ),
                        );
                      },
                    ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _scan() async {
    try {
      final value = await Navigator.push<String>(
        context,
        MaterialPageRoute(builder: (_) => const ScannerPage()),
      );
      if (value == null || !mounted) return;
      _searchController.text = value;
      await context.read<ProductsCubit>().scan(value);
    } catch (error) {
      if (mounted) showAppMessage(context, '$error', isError: true);
    }
  }
}
