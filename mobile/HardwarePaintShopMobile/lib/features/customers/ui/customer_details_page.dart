import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/core/utils/formatters.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/app_message.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/error_banner.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/info_card.dart';
import 'package:hardware_paint_shop_mobile/features/auth/logic/auth_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/customers/data/models/customer_summary.dart';
import 'package:hardware_paint_shop_mobile/features/customers/data/repositories/customers_repository.dart';
import 'package:hardware_paint_shop_mobile/features/customers/logic/customer_details_cubit.dart';

class CustomerDetailsPage extends StatelessWidget {
  const CustomerDetailsPage({super.key, required this.customer});

  final CustomerSummary customer;

  @override
  Widget build(BuildContext context) => BlocProvider(
        create: (context) => CustomerDetailsCubit(
          context.read<CustomersRepository>(),
        )..load(customer.id),
        child: _CustomerDetailsView(customer: customer),
      );
}

class _CustomerDetailsView extends StatelessWidget {
  const _CustomerDetailsView({required this.customer});

  final CustomerSummary customer;

  @override
  Widget build(BuildContext context) {
    final canCollect = context.select(
      (AuthCubit cubit) => cubit.state.can('Finance.CustomerCollection'),
    );
    return BlocConsumer<CustomerDetailsCubit, CustomerDetailsState>(
      listenWhen: (previous, current) =>
          previous.message != current.message && current.message != null,
      listener: (context, state) => showAppMessage(
        context,
        state.message!,
        isError: state.status == ViewStatus.failure,
      ),
      builder: (context, state) {
        final statement = state.statement;
        return Scaffold(
          appBar: AppBar(title: Text(customer.name)),
          floatingActionButton: canCollect && statement != null
              ? FloatingActionButton.extended(
                  onPressed: () => _showCollection(context, state),
                  icon: const Icon(Icons.payments),
                  label: const Text('تحصيل'),
                )
              : null,
          body: statement == null
              ? Center(
                  child: state.status == ViewStatus.failure
                      ? ErrorBanner(state.message ?? 'تعذر تحميل كشف الحساب.')
                      : const CircularProgressIndicator(),
                )
              : RefreshIndicator(
                  onRefresh: () =>
                      context.read<CustomerDetailsCubit>().load(customer.id),
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      InfoCard(
                        children: [
                          InfoRow(
                            label: 'الهاتف',
                            value: statement.phone ?? '—',
                          ),
                          InfoRow(
                            label: 'الرصيد الحالي',
                            value: money(statement.currentBalance),
                          ),
                          InfoRow(
                            label: 'حد الائتمان',
                            value: money(statement.creditLimit),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),
                      Text(
                        'آخر الفواتير',
                        style: Theme.of(context)
                            .textTheme
                            .titleMedium
                            ?.copyWith(fontWeight: FontWeight.w800),
                      ),
                      const SizedBox(height: 8),
                      ...statement.invoices.take(30).map(
                            (invoice) => Card(
                              child: ListTile(
                                title: Text(invoice.invoiceNumber),
                                subtitle: Text(
                                  invoice.invoiceDate == null
                                      ? '—'
                                      : '${invoice.invoiceDate!.day}/${invoice.invoiceDate!.month}/${invoice.invoiceDate!.year}',
                                ),
                                trailing: Column(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  crossAxisAlignment: CrossAxisAlignment.end,
                                  children: [
                                    Text(
                                      money(invoice.totalAmount),
                                      style: const TextStyle(
                                        fontWeight: FontWeight.w700,
                                      ),
                                    ),
                                    Text(
                                      'متبقي ${money(invoice.remainingAmount)}',
                                      style: const TextStyle(
                                        fontSize: 11,
                                        color: Color(0xffdc2626),
                                      ),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ),
                    ],
                  ),
                ),
        );
      },
    );
  }

  Future<void> _showCollection(
    BuildContext context,
    CustomerDetailsState state,
  ) async {
    final amountController = TextEditingController();
    final notesController = TextEditingController();
    final cubit = context.read<CustomerDetailsCubit>();
    String? cashboxId =
        state.cashboxes.isEmpty ? null : state.cashboxes.first.id;
    try {
      await showDialog<void>(
        context: context,
        builder: (dialogContext) => StatefulBuilder(
          builder: (context, setDialogState) => AlertDialog(
            title: Text('تحصيل من ${customer.name}'),
            content: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text('الرصيد الحالي: ${money(customer.balance)}'),
                const SizedBox(height: 12),
                TextField(
                  controller: amountController,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(labelText: 'المبلغ'),
                ),
                const SizedBox(height: 10),
                DropdownButtonFormField<String>(
                  initialValue: cashboxId,
                  decoration: const InputDecoration(labelText: 'الخزينة'),
                  items: state.cashboxes
                      .map(
                        (cashbox) => DropdownMenuItem(
                          value: cashbox.id,
                          child: Text(cashbox.name),
                        ),
                      )
                      .toList(),
                  onChanged: (value) =>
                      setDialogState(() => cashboxId = value),
                ),
                const SizedBox(height: 10),
                TextField(
                  controller: notesController,
                  decoration: const InputDecoration(labelText: 'ملاحظات'),
                ),
              ],
            ),
            actions: [
              TextButton(
                onPressed: () => Navigator.pop(dialogContext),
                child: const Text('إلغاء'),
              ),
              FilledButton(
                onPressed: cashboxId == null
                    ? null
                    : () {
                        cubit.collect(
                              amount:
                                  double.tryParse(amountController.text) ?? 0,
                              cashboxId: cashboxId!,
                              notes: notesController.text,
                            );
                        Navigator.pop(dialogContext);
                      },
                child: const Text('حفظ'),
              ),
            ],
          ),
        ),
      );
    } catch (error) {
      if (context.mounted) showAppMessage(context, '$error', isError: true);
    } finally {
      amountController.dispose();
      notesController.dispose();
    }
  }
}
