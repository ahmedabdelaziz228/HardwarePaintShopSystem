import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:hardware_paint_shop_mobile/core/app/shop_mobile_app.dart';
import 'package:hardware_paint_shop_mobile/core/network/api_client.dart';
import 'package:hardware_paint_shop_mobile/features/auth/data/repositories/auth_repository.dart';
import 'package:hardware_paint_shop_mobile/features/auth/logic/auth_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/auth/ui/auth_gate.dart';
import 'package:hardware_paint_shop_mobile/features/dashboard/data/models/dashboard_summary.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/models/product_summary.dart';
import 'package:hardware_paint_shop_mobile/features/shell/data/models/shell_destination.dart';
import 'package:hardware_paint_shop_mobile/features/shell/logic/shell_cubit.dart';

void main() {
  testWidgets('shows loading screen before authentication initialization', (
    tester,
  ) async {
    final authCubit = AuthCubit(AuthRepository(ApiClient()));
    await tester.pumpWidget(
      BlocProvider.value(
        value: authCubit,
        child: const ShopMobileApp(),
      ),
    );

    expect(find.byType(SplashPage), findsOneWidget);
    await authCubit.close();
  });

  test('product summary parses stock and price defensively', () {
    final product = ProductSummary.fromJson({
      'id': 'p1',
      'name': 'سلك',
      'stockBase': '37.5',
      'defaultPrice': 50,
      'minStockBaseQuantity': 10,
    });

    expect(product.stockBase, 37.5);
    expect(product.defaultPrice, 50);
    expect(product.isLow, isFalse);
  });

  test('dashboard model parses numeric API values', () {
    final dashboard = DashboardSummary.fromJson({
      'salesToday': '100.5',
      'lowStockCount': 3,
    });

    expect(dashboard.salesToday, 100.5);
    expect(dashboard.lowStockCount, 3);
  });

  test('shell cubit limits navigation index', () async {
    final cubit = ShellCubit();
    cubit.selectPage(99);
    expect(cubit.state, ShellDestination.sync);
    await cubit.close();
  });

  test('Dio API base URL normalization is stable', () {
    expect(
      ApiClient.normalizeBaseUrl('192.168.1.7:5000/'),
      'http://192.168.1.7:5000',
    );
  });
}
