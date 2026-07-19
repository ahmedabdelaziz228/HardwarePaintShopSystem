import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:hardware_paint_shop_mobile/core/state/view_status.dart';
import 'package:hardware_paint_shop_mobile/core/utils/formatters.dart';
import 'package:hardware_paint_shop_mobile/core/widgets/error_banner.dart';
import 'package:hardware_paint_shop_mobile/features/customers/logic/customers_cubit.dart';
import 'package:hardware_paint_shop_mobile/features/customers/ui/customer_details_page.dart';

class CustomersPage extends StatefulWidget {
  const CustomersPage({super.key});

  @override
  State<CustomersPage> createState() => _CustomersPageState();
}

class _CustomersPageState extends State<CustomersPage> {
  final _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    if (context.read<CustomersCubit>().state.status == ViewStatus.initial) {
      context.read<CustomersCubit>().search('');
    }
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) =>
      BlocBuilder<CustomersCubit, CustomersState>(
        builder: (context, state) => Column(
          children: [
            Padding(
              padding: const EdgeInsets.all(16),
              child: TextField(
                controller: _searchController,
                onSubmitted: context.read<CustomersCubit>().search,
                decoration: InputDecoration(
                  hintText: 'اسم العميل أو الهاتف',
                  prefixIcon: const Icon(Icons.search),
                  suffixIcon: IconButton(
                    tooltip: 'بحث',
                    onPressed: () => context
                        .read<CustomersCubit>()
                        .search(_searchController.text),
                    icon: const Icon(Icons.arrow_forward),
                  ),
                ),
              ),
            ),
            if (state.message != null)
              Padding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                child: ErrorBanner(state.message!),
              ),
            if (state.status == ViewStatus.loading && state.customers.isNotEmpty)
              const LinearProgressIndicator(),
            Expanded(
              child: state.status == ViewStatus.loading && state.customers.isEmpty
                  ? const Center(child: CircularProgressIndicator())
                  : ListView.separated(
                      padding: const EdgeInsets.fromLTRB(16, 8, 16, 20),
                      itemCount: state.customers.length,
                      separatorBuilder: (_, __) => const SizedBox(height: 9),
                      itemBuilder: (_, index) {
                        final customer = state.customers[index];
                        return Card(
                          child: ListTile(
                            leading: CircleAvatar(
                              child: Text(
                                customer.name.isEmpty ? '?' : customer.name[0],
                              ),
                            ),
                            title: Text(
                              customer.name,
                              style: const TextStyle(fontWeight: FontWeight.w700),
                            ),
                            subtitle: Text(customer.phone ?? 'بدون هاتف'),
                            trailing: Text(
                              money(customer.balance),
                              style: TextStyle(
                                fontWeight: FontWeight.w800,
                                color: customer.balance > 0
                                    ? const Color(0xffdc2626)
                                    : const Color(0xff059669),
                              ),
                            ),
                            onTap: () => Navigator.push(
                              context,
                              MaterialPageRoute<void>(
                                builder: (_) =>
                                    CustomerDetailsPage(customer: customer),
                              ),
                            ),
                          ),
                        );
                      },
                    ),
            ),
          ],
        ),
      );
}
