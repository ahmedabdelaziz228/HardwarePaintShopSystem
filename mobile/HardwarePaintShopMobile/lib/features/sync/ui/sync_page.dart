import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/app_message.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/error_banner.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/info_card.dart';
import 'package:hardware_paint_shop_mobile/features/auth/logic/auth_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/sync/logic/sync_cubit.dart';
import 'package:intl/intl.dart' as intl;

class SyncPage extends StatelessWidget {
  const SyncPage({super.key});

  @override
  Widget build(BuildContext context) => BlocConsumer<SyncCubit, SyncState>(
        listenWhen: (previous, current) =>
            previous.message != current.message && current.message != null,
        listener: (context, state) => showAppMessage(
          context,
          state.message!,
          isError: state.status == ViewStatus.failure,
        ),
        builder: (context, state) {
          final isBusy = state.status == ViewStatus.loading;
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              InfoCard(
                children: [
                  InfoRow(
                    label: 'عنوان API',
                    value: context.read<SyncCubit>().baseUrl,
                  ),
                  InfoRow(
                    label: 'آخر مزامنة',
                    value: state.lastSync == null
                        ? 'لم تتم'
                        : intl.DateFormat('dd/MM/yyyy HH:mm')
                            .format(state.lastSync!.toLocal()),
                  ),
                  InfoRow(
                    label: 'عمليات معلقة',
                    value: '${state.pending.length}',
                  ),
                  InfoRow(
                    label: 'الحالة',
                    value: state.online ? 'متصل' : 'أوفلاين',
                  ),
                ],
              ),
              const SizedBox(height: 14),
              FilledButton.icon(
                onPressed: isBusy
                    ? null
                    : context.read<SyncCubit>().synchronize,
                icon: isBusy
                    ? const SizedBox.square(
                        dimension: 18,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Icon(Icons.sync),
                label: const Padding(
                  padding: EdgeInsets.symmetric(vertical: 13),
                  child: Text('مزامنة الآن'),
                ),
              ),
              if (state.status == ViewStatus.failure && state.message != null)
                Padding(
                  padding: const EdgeInsets.only(top: 12),
                  child: ErrorBanner(state.message!),
                ),
              const SizedBox(height: 16),
              Card(
                child: ListTile(
                  leading: const Icon(Icons.logout, color: Color(0xffdc2626)),
                  title: const Text('تسجيل الخروج'),
                  onTap: context.read<AuthCubit>().logout,
                ),
              ),
              const Padding(
                padding: EdgeInsets.all(16),
                child: Text(
                  'Hardware Paint Shop Mobile • BLoC Clean Architecture',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: Color(0xff94a3b8), fontSize: 12),
                ),
              ),
            ],
          );
        },
      );
}
