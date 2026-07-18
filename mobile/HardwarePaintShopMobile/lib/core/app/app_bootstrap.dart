import 'package:flutter/widgets.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/app/shop_mobile_app.dart';
import 'package:hardware_paint_shop_mobile/core/network/api_client.dart';
import 'package:hardware_paint_shop_mobile/core/storage/app_database.dart';
import 'package:hardware_paint_shop_mobile/core/storage/offline_operation_queue.dart';
import 'package:hardware_paint_shop_mobile/features/alerts/data/repositories/alerts_repository.dart';
import 'package:hardware_paint_shop_mobile/features/alerts/logic/alerts_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/auth/data/repositories/auth_repository.dart';
import 'package:hardware_paint_shop_mobile/features/auth/logic/auth_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/customers/data/repositories/customers_repository.dart';
import 'package:hardware_paint_shop_mobile/features/customers/logic/customers_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/dashboard/data/repositories/dashboard_repository.dart';
import 'package:hardware_paint_shop_mobile/features/dashboard/logic/dashboard_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/repositories/products_repository.dart';
import 'package:hardware_paint_shop_mobile/features/products/logic/products_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/shell/logic/shell_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/sync/data/repositories/sync_repository.dart';
import 'package:hardware_paint_shop_mobile/features/sync/logic/sync_cubit.dart';

/// Composes core services, feature repositories, and application Cubits.
Future<void> bootstrapApp() async {
  WidgetsFlutterBinding.ensureInitialized();

  final apiClient = ApiClient();
  final database = AppDatabase();
  final operationQueue = OfflineOperationQueue(database);

  final authRepository = AuthRepository(apiClient);
  final dashboardRepository = DashboardRepository(apiClient);
  final productsRepository = ProductsRepository(
    apiClient,
    database,
    operationQueue,
  );
  final customersRepository = CustomersRepository(
    apiClient,
    database,
    operationQueue,
  );
  final alertsRepository = AlertsRepository(apiClient, database);
  final syncRepository = SyncRepository(apiClient, database);

  runApp(
    MultiRepositoryProvider(
      providers: [
        RepositoryProvider.value(value: apiClient),
        RepositoryProvider.value(value: database),
        RepositoryProvider.value(value: authRepository),
        RepositoryProvider.value(value: dashboardRepository),
        RepositoryProvider.value(value: productsRepository),
        RepositoryProvider.value(value: customersRepository),
        RepositoryProvider.value(value: alertsRepository),
        RepositoryProvider.value(value: syncRepository),
      ],
      child: MultiBlocProvider(
        providers: [
          BlocProvider(
            lazy: false,
            create: (_) => AuthCubit(authRepository)..initialize(),
          ),
          BlocProvider(create: (_) => DashboardCubit(dashboardRepository)),
          BlocProvider(create: (_) => ProductsCubit(productsRepository)),
          BlocProvider(create: (_) => CustomersCubit(customersRepository)),
          BlocProvider(create: (_) => AlertsCubit(alertsRepository)),
          BlocProvider(
            lazy: false,
            create: (_) => SyncCubit(syncRepository)..initialize(),
          ),
          BlocProvider(create: (_) => ShellCubit()),
        ],
        child: const ShopMobileApp(),
      ),
    ),
  );
}
