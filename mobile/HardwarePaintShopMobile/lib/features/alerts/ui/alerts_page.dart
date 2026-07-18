import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/error_banner.dart';
import 'package:hardware_paint_shop_mobile/features/alerts/logic/alerts_cubit.dart';

class AlertsPage extends StatelessWidget {
  const AlertsPage({super.key});

  @override
  Widget build(BuildContext context) => BlocBuilder<AlertsCubit, AlertsState>(
        builder: (context, state) => RefreshIndicator(
          onRefresh: context.read<AlertsCubit>().load,
          child: ListView.separated(
            padding: const EdgeInsets.all(16),
            itemCount: state.alerts.length + (state.message == null ? 0 : 1),
            separatorBuilder: (_, __) => const SizedBox(height: 9),
            itemBuilder: (_, index) {
              if (state.message != null && index == 0) {
                return ErrorBanner(state.message!);
              }
              final alertIndex = index - (state.message == null ? 0 : 1);
              final alert = state.alerts[alertIndex];
              final critical = alert.severity.toLowerCase() == 'critical';
              return Card(
                child: ListTile(
                  leading: CircleAvatar(
                    backgroundColor: critical
                        ? const Color(0xffffe4e6)
                        : const Color(0xfffff7ed),
                    child: Icon(
                      critical ? Icons.error_outline : Icons.warning_amber,
                      color: critical
                          ? const Color(0xffdc2626)
                          : const Color(0xffd97706),
                    ),
                  ),
                  title: Text(
                    alert.title,
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                  subtitle: Text(alert.message),
                  trailing: alert.isRead
                      ? null
                      : IconButton(
                          tooltip: 'تعليم كمقروء',
                          onPressed: () => context
                              .read<AlertsCubit>()
                              .markRead(alert.id),
                          icon: const Icon(Icons.done),
                        ),
                ),
              );
            },
          ),
        ),
      );
}
