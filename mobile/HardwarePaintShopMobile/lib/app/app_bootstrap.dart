import 'package:flutter/widgets.dart';
import 'package:hardware_paint_shop_mobile/app/app.dart';
import 'package:hardware_paint_shop_mobile/app_state.dart';
import 'package:hardware_paint_shop_mobile/core/network/api_client.dart';
import 'package:hardware_paint_shop_mobile/data/local/app_database.dart';
import 'package:hardware_paint_shop_mobile/features/auth/data/auth_repository.dart';
import 'package:hardware_paint_shop_mobile/features/auth/logic/auth_controller.dart';
import 'package:provider/provider.dart';

/// Creates long-lived services and controllers before rendering the application.
Future<void> bootstrapApp() async {
  WidgetsFlutterBinding.ensureInitialized();

  final apiClient = ApiClient();
  final database = AppDatabase();
  final authController = AuthController(AuthRepository(apiClient));
  final appState = AppState(apiClient, database);

  runApp(
    MultiProvider(
      providers: [
        ChangeNotifierProvider.value(value: authController),
        ChangeNotifierProvider.value(value: appState),
      ],
      child: const ShopMobileApp(),
    ),
  );

  await Future.wait([
    authController.initialize(),
    appState.initializeLocalState(),
  ]);
}
