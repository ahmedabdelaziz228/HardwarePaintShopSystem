import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/utils/formatters.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/error_banner.dart';
import 'package:hardware_paint_shop_mobile/features/auth/logic/auth_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/dashboard/logic/dashboard_cubit.dart';

class DashboardPage extends StatelessWidget {
  const DashboardPage({super.key});

  @override
  Widget build(BuildContext context) => BlocBuilder<DashboardCubit, DashboardState>(
        builder: (context, state) {
          final userName = context.select(
            (AuthCubit cubit) => cubit.state.userName,
          );
          final dashboard = state.summary;
          return RefreshIndicator(
            onRefresh: () => context.read<DashboardCubit>().load(),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                Text(
                  'أهلًا، $userName',
                  style: Theme.of(context)
                      .textTheme
                      .titleLarge
                      ?.copyWith(fontWeight: FontWeight.w800),
                ),
                const SizedBox(height: 4),
                const Text(
                  'ملخص سريع عن حركة المحل اليوم',
                  style: TextStyle(color: Color(0xff64748b)),
                ),
                const SizedBox(height: 16),
                LayoutBuilder(
                  builder: (context, constraints) => GridView.count(
                    crossAxisCount: constraints.maxWidth >= 700 ? 3 : 2,
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    mainAxisSpacing: 12,
                    crossAxisSpacing: 12,
                    childAspectRatio: constraints.maxWidth >= 700 ? 1.8 : 1.35,
                    children: [
                      MetricCard(
                        label: 'مبيعات اليوم',
                        value: money(dashboard.salesToday),
                        icon: Icons.point_of_sale,
                        color: const Color(0xff2563eb),
                      ),
                      MetricCard(
                        label: 'التحصيلات',
                        value: money(dashboard.collectedToday),
                        icon: Icons.payments_outlined,
                        color: const Color(0xff059669),
                      ),
                      MetricCard(
                        label: 'المصروفات',
                        value: money(dashboard.expensesToday),
                        icon: Icons.receipt_long_outlined,
                        color: const Color(0xffdc2626),
                      ),
                      MetricCard(
                        label: 'رصيد الخزائن',
                        value: money(dashboard.cashBalance),
                        icon: Icons.account_balance_wallet_outlined,
                        color: const Color(0xff7c3aed),
                      ),
                      MetricCard(
                        label: 'ديون العملاء',
                        value: money(dashboard.customerDebt),
                        icon: Icons.credit_score,
                        color: const Color(0xffd97706),
                      ),
                      MetricCard(
                        label: 'منتجات ناقصة',
                        value: '${dashboard.lowStockCount}',
                        icon: Icons.warning_amber,
                        color: const Color(0xffea580c),
                      ),
                    ],
                  ),
                ),
                if (state.errorMessage != null)
                  Padding(
                    padding: const EdgeInsets.only(top: 14),
                    child: ErrorBanner(state.errorMessage!),
                  ),
              ],
            ),
          );
        },
      );
}

class MetricCard extends StatelessWidget {
  const MetricCard({
    super.key,
    required this.label,
    required this.value,
    required this.icon,
    required this.color,
  });

  final String label;
  final String value;
  final IconData icon;
  final Color color;

  @override
  Widget build(BuildContext context) => Card(
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              CircleAvatar(
                radius: 18,
                backgroundColor: color.withValues(alpha: .12),
                child: Icon(icon, color: color, size: 20),
              ),
              const Spacer(),
              Text(
                value,
                maxLines: 1,
                style: const TextStyle(
                  fontSize: 18,
                  fontWeight: FontWeight.w800,
                ),
              ),
              Text(
                label,
                style: const TextStyle(
                  fontSize: 12,
                  color: Color(0xff64748b),
                ),
              ),
            ],
          ),
        ),
      );
}
