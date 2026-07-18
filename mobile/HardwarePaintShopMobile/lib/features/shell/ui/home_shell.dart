import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/features/alerts/logic/alerts_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/alerts/ui/alerts_page.dart';
import 'package:hardware_paint_shop_mobile/features/customers/ui/customers_page.dart';
import 'package:hardware_paint_shop_mobile/features/dashboard/logic/dashboard_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/dashboard/ui/dashboard_page.dart';
import 'package:hardware_paint_shop_mobile/features/products/ui/products_page.dart';
import 'package:hardware_paint_shop_mobile/features/shell/data/models/shell_destination.dart';
import 'package:hardware_paint_shop_mobile/features/shell/logic/shell_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/sync/logic/sync_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/sync/ui/sync_page.dart';

class HomeShell extends StatefulWidget {
  const HomeShell({super.key});

  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  static const _pages = [
    DashboardPage(),
    ProductsPage(),
    CustomersPage(),
    AlertsPage(),
    SyncPage(),
  ];

  @override
  void initState() {
    super.initState();
    context.read<DashboardCubit>().load();
    context.read<AlertsCubit>().load();
    context.read<SyncCubit>().refreshPending();
  }

  @override
  Widget build(BuildContext context) =>
      BlocBuilder<ShellCubit, ShellDestination>(
        builder: (context, destination) {
          final selectedIndex = destination.index;
          final dashboard = context.watch<DashboardCubit>().state;
          final sync = context.watch<SyncCubit>().state;
          final online = dashboard.online || sync.online;
          return Scaffold(
            appBar: AppBar(
              title: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    dashboard.shopName,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w700,
                    ),
                  ),
                  Text(
                    online ? 'متصل باللاب' : 'وضع أوفلاين',
                    style: TextStyle(
                      fontSize: 11,
                      color: online
                          ? const Color(0xff059669)
                          : const Color(0xffd97706),
                    ),
                  ),
                ],
              ),
              actions: [
                if (sync.pending.isNotEmpty)
                  Badge(
                    label: Text('${sync.pending.length}'),
                    child: IconButton(
                      tooltip: 'عمليات معلقة',
                      onPressed: () => context.read<ShellCubit>().selectPage(4),
                      icon: const Icon(Icons.sync_problem),
                    ),
                  ),
                IconButton(
                  tooltip: 'تحديث',
                  onPressed: () {
                    context.read<DashboardCubit>().load(showLoader: false);
                    context.read<AlertsCubit>().load();
                  },
                  icon: const Icon(Icons.refresh),
                ),
              ],
            ),
            body: IndexedStack(index: selectedIndex, children: _pages),
            bottomNavigationBar: NavigationBar(
              selectedIndex: selectedIndex,
              onDestinationSelected: context.read<ShellCubit>().selectPage,
              destinations: const [
                NavigationDestination(
                  icon: Icon(Icons.dashboard_outlined),
                  selectedIcon: Icon(Icons.dashboard),
                  label: 'الرئيسية',
                ),
                NavigationDestination(
                  icon: Icon(Icons.inventory_2_outlined),
                  selectedIcon: Icon(Icons.inventory_2),
                  label: 'المنتجات',
                ),
                NavigationDestination(
                  icon: Icon(Icons.groups_outlined),
                  selectedIcon: Icon(Icons.groups),
                  label: 'العملاء',
                ),
                NavigationDestination(
                  icon: Icon(Icons.notifications_outlined),
                  selectedIcon: Icon(Icons.notifications),
                  label: 'التنبيهات',
                ),
                NavigationDestination(
                  icon: Icon(Icons.more_horiz),
                  label: 'المزيد',
                ),
              ],
            ),
          );
        },
      );
}
