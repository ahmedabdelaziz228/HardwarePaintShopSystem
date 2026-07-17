import 'package:flutter_test/flutter_test.dart';
import 'package:hardware_paint_shop_mobile/api_client.dart';
import 'package:hardware_paint_shop_mobile/app_state.dart';
import 'package:hardware_paint_shop_mobile/local_store.dart';
import 'package:hardware_paint_shop_mobile/main.dart';
import 'package:provider/provider.dart';

void main() {
  testWidgets('shows loading screen before initialization', (tester) async {
    final state = AppState(ApiClient(), LocalStore());
    await tester.pumpWidget(
      ChangeNotifierProvider.value(value: state, child: const ShopMobileApp()),
    );
    expect(find.byType(SplashScreen), findsOneWidget);
  });
}
