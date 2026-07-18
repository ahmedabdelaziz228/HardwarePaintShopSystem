import 'package:flutter_test/flutter_test.dart';
import 'package:hardware_paint_shop_mobile/app/app.dart';
import 'package:hardware_paint_shop_mobile/app_state.dart';
import 'package:hardware_paint_shop_mobile/core/network/api_client.dart';
import 'package:hardware_paint_shop_mobile/data/local/app_database.dart';
import 'package:hardware_paint_shop_mobile/features/auth/data/auth_repository.dart';
import 'package:hardware_paint_shop_mobile/features/auth/logic/auth_controller.dart';
import 'package:hardware_paint_shop_mobile/routes/auth_gate.dart';
import 'package:provider/provider.dart';

void main() {
  testWidgets('shows loading screen before initialization', (tester) async {
    final apiClient = ApiClient();
    final state = AppState(apiClient, AppDatabase());
    final auth = AuthController(AuthRepository(apiClient));
    await tester.pumpWidget(
      MultiProvider(
        providers: [
          ChangeNotifierProvider.value(value: state),
          ChangeNotifierProvider.value(value: auth),
        ],
        child: const ShopMobileApp(),
      ),
    );
    expect(find.byType(SplashScreen), findsOneWidget);
  });
}
