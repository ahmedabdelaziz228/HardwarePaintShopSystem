import 'dart:convert';
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:intl/intl.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:provider/provider.dart';

import 'api_client.dart';
import 'app_state.dart';
import 'local_store.dart';
import 'models.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final state = AppState(ApiClient(), LocalStore());
  await state.initialize();
  runApp(ChangeNotifierProvider.value(value: state, child: const ShopMobileApp()));
}

class ShopMobileApp extends StatelessWidget {
  const ShopMobileApp({super.key});

  @override
  Widget build(BuildContext context) {
    final colors = ColorScheme.fromSeed(seedColor: const Color(0xff2563eb), brightness: Brightness.light);
    return MaterialApp(
      debugShowCheckedModeBanner: false,
      title: 'إدارة المحل',
      locale: const Locale('ar'),
      theme: ThemeData(
        useMaterial3: true,
        colorScheme: colors,
        scaffoldBackgroundColor: const Color(0xfff5f7fb),
        inputDecorationTheme: InputDecorationTheme(
          filled: true,
          fillColor: Colors.white,
          border: OutlineInputBorder(borderRadius: BorderRadius.circular(14), borderSide: BorderSide.none),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(14),
            borderSide: const BorderSide(color: Color(0xffe2e8f0)),
          ),
        ),
      ),
      builder: (context, child) => Directionality(textDirection: TextDirection.rtl, child: child!),
      home: Consumer<AppState>(builder: (context, state, _) {
        if (!state.initialized) return const SplashScreen();
        return state.authenticated ? const HomeShell() : const LoginScreen();
      }),
    );
  }
}

class SplashScreen extends StatelessWidget {
  const SplashScreen({super.key});
  @override
  Widget build(BuildContext context) => const Scaffold(body: Center(child: CircularProgressIndicator()));
}

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});
  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  late final TextEditingController url;
  final username = TextEditingController();
  final password = TextEditingController();
  bool obscure = true;

  @override
  void initState() {
    super.initState();
    url = TextEditingController(text: context.read<AppState>().api.baseUrl);
  }

  @override
  Widget build(BuildContext context) {
    final state = context.watch<AppState>();
    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 440),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Container(
                    width: 84,
                    height: 84,
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      color: Theme.of(context).colorScheme.primary,
                      borderRadius: BorderRadius.circular(24),
                      boxShadow: const [BoxShadow(color: Color(0x332563eb), blurRadius: 24, offset: Offset(0, 10))],
                    ),
                    child: const Text('HP', style: TextStyle(color: Colors.white, fontSize: 28, fontWeight: FontWeight.w800)),
                  ),
                  const SizedBox(height: 24),
                  Text('اتصل بالمحل', style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.w800)),
                  const SizedBox(height: 6),
                  const Text('الهاتف واللاب لازم يكونوا على نفس شبكة الواي فاي.', style: TextStyle(color: Color(0xff64748b))),
                  const SizedBox(height: 28),
                  TextField(controller: url, textDirection: TextDirection.ltr, decoration: const InputDecoration(labelText: 'عنوان API', hintText: 'http://192.168.1.10:5000', prefixIcon: Icon(Icons.lan_outlined))),
                  const SizedBox(height: 12),
                  TextField(controller: username, decoration: const InputDecoration(labelText: 'اسم المستخدم', prefixIcon: Icon(Icons.person_outline))),
                  const SizedBox(height: 12),
                  TextField(
                    controller: password,
                    obscureText: obscure,
                    decoration: InputDecoration(
                      labelText: 'كلمة المرور',
                      prefixIcon: const Icon(Icons.lock_outline),
                      suffixIcon: IconButton(onPressed: () => setState(() => obscure = !obscure), icon: Icon(obscure ? Icons.visibility_outlined : Icons.visibility_off_outlined)),
                    ),
                  ),
                  if (state.error != null) ...[
                    const SizedBox(height: 12),
                    ErrorBanner(state.error!),
                  ],
                  const SizedBox(height: 18),
                  FilledButton.icon(
                    onPressed: state.busy ? null : () async {
                      try {
                        await state.login(url.text, username.text, password.text);
                      } catch (_) {}
                    },
                    icon: state.busy ? const SizedBox.square(dimension: 18, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.login),
                    label: const Padding(padding: EdgeInsets.symmetric(vertical: 14), child: Text('تسجيل الدخول')),
                  ),
                  const SizedBox(height: 14),
                  const Text('استخدم IP اللاب، وليس localhost. غيّر كلمة مرور admin من الكمبيوتر قبل دخول الموبايل.', textAlign: TextAlign.center, style: TextStyle(fontSize: 12, color: Color(0xff64748b))),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class HomeShell extends StatefulWidget {
  const HomeShell({super.key});
  @override
  State<HomeShell> createState() => _HomeShellState();
}

class _HomeShellState extends State<HomeShell> {
  int index = 0;
  final pages = const [DashboardPage(), ProductsPage(), CustomersPage(), AlertsPage(), MorePage()];

  @override
  Widget build(BuildContext context) {
    final state = context.watch<AppState>();
    return Scaffold(
      appBar: AppBar(
        title: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(state.shopName, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
          Text(state.online ? 'متصل باللاب' : 'وضع أوفلاين', style: TextStyle(fontSize: 11, color: state.online ? const Color(0xff059669) : const Color(0xffd97706))),
        ]),
        actions: [
          if (state.pending.isNotEmpty)
            Badge(label: Text('${state.pending.length}'), child: IconButton(onPressed: () => setState(() => index = 4), icon: const Icon(Icons.sync_problem))),
          IconButton(onPressed: () => state.refreshHome(), icon: const Icon(Icons.refresh)),
        ],
      ),
      body: IndexedStack(index: index, children: pages),
      bottomNavigationBar: NavigationBar(
        selectedIndex: index,
        onDestinationSelected: (value) => setState(() => index = value),
        destinations: const [
          NavigationDestination(icon: Icon(Icons.dashboard_outlined), selectedIcon: Icon(Icons.dashboard), label: 'الرئيسية'),
          NavigationDestination(icon: Icon(Icons.inventory_2_outlined), selectedIcon: Icon(Icons.inventory_2), label: 'المنتجات'),
          NavigationDestination(icon: Icon(Icons.groups_outlined), selectedIcon: Icon(Icons.groups), label: 'العملاء'),
          NavigationDestination(icon: Icon(Icons.notifications_outlined), selectedIcon: Icon(Icons.notifications), label: 'التنبيهات'),
          NavigationDestination(icon: Icon(Icons.more_horiz), label: 'المزيد'),
        ],
      ),
    );
  }
}

class DashboardPage extends StatelessWidget {
  const DashboardPage({super.key});
  @override
  Widget build(BuildContext context) {
    final state = context.watch<AppState>();
    final d = state.dashboard;
    return RefreshIndicator(
      onRefresh: state.refreshHome,
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Text('أهلًا، ${state.userName}', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w800)),
          const SizedBox(height: 4),
          const Text('ملخص سريع عن حركة المحل اليوم', style: TextStyle(color: Color(0xff64748b))),
          const SizedBox(height: 16),
          GridView.count(
            crossAxisCount: 2,
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            mainAxisSpacing: 12,
            crossAxisSpacing: 12,
            childAspectRatio: 1.35,
            children: [
              MetricCard('مبيعات اليوم', money(d['salesToday']), Icons.point_of_sale, const Color(0xff2563eb)),
              MetricCard('التحصيلات', money(d['collectedToday']), Icons.payments_outlined, const Color(0xff059669)),
              MetricCard('المصروفات', money(d['expensesToday']), Icons.receipt_long_outlined, const Color(0xffdc2626)),
              MetricCard('رصيد الخزائن', money(d['cashBalance']), Icons.account_balance_wallet_outlined, const Color(0xff7c3aed)),
              MetricCard('ديون العملاء', money(d['customerDebt']), Icons.credit_score, const Color(0xffd97706)),
              MetricCard('منتجات ناقصة', '${d['lowStockCount'] ?? 0}', Icons.warning_amber, const Color(0xffea580c)),
            ],
          ),
          if (state.error != null) Padding(padding: const EdgeInsets.only(top: 14), child: ErrorBanner(state.error!)),
        ],
      ),
    );
  }
}

class ProductsPage extends StatefulWidget {
  const ProductsPage({super.key});
  @override
  State<ProductsPage> createState() => _ProductsPageState();
}

class _ProductsPageState extends State<ProductsPage> {
  final search = TextEditingController();
  @override
  void initState() {
    super.initState();
    Future.microtask(() => context.read<AppState>().searchProducts(''));
  }

  @override
  Widget build(BuildContext context) {
    final state = context.watch<AppState>();
    return Scaffold(
      backgroundColor: Colors.transparent,
      floatingActionButton: state.permissions.contains('Product.Create')
          ? FloatingActionButton.extended(
              onPressed: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const AddProductPage())),
              icon: const Icon(Icons.add), label: const Text('منتج'))
          : null,
      body: Column(children: [
        Padding(
          padding: const EdgeInsets.all(16),
          child: TextField(
            controller: search,
            onSubmitted: state.searchProducts,
            decoration: InputDecoration(
              hintText: 'اسم، كود، باركود أو سيريال',
              prefixIcon: const Icon(Icons.search),
              suffixIcon: Row(mainAxisSize: MainAxisSize.min, children: [
                IconButton(onPressed: () => state.searchProducts(search.text), icon: const Icon(Icons.arrow_forward)),
                IconButton(onPressed: () async {
                  final value = await Navigator.push<String>(context, MaterialPageRoute(builder: (_) => const ScannerPage()));
                  if (value != null && context.mounted) {
                    search.text = value;
                    try { await state.scanLookup(value); } catch (_) {}
                  }
                }, icon: const Icon(Icons.qr_code_scanner)),
              ]),
            ),
          ),
        ),
        if (state.error != null) Padding(padding: const EdgeInsets.symmetric(horizontal: 16), child: ErrorBanner(state.error!)),
        Expanded(
          child: ListView.separated(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 100),
            itemCount: state.products.length,
            separatorBuilder: (_, __) => const SizedBox(height: 9),
            itemBuilder: (_, i) => ProductTile(product: state.products[i]),
          ),
        ),
      ]),
    );
  }
}

class ProductTile extends StatelessWidget {
  const ProductTile({super.key, required this.product});
  final ProductSummary product;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => ProductDetailsPage(product: product))),
        child: Padding(
          padding: const EdgeInsets.all(13),
          child: Row(children: [
            Container(
              width: 48, height: 48, alignment: Alignment.center,
              decoration: BoxDecoration(color: const Color(0xffeff6ff), borderRadius: BorderRadius.circular(12)),
              child: const Icon(Icons.inventory_2_outlined, color: Color(0xff2563eb)),
            ),
            const SizedBox(width: 12),
            Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(product.name, style: const TextStyle(fontWeight: FontWeight.w700)),
              const SizedBox(height: 3),
              Text([product.code, product.category].whereType<String>().join(' • '), style: const TextStyle(fontSize: 12, color: Color(0xff64748b))),
            ])),
            Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
              Text(product.defaultPrice == null ? 'بدون سعر' : money(product.defaultPrice), style: const TextStyle(fontWeight: FontWeight.w800, color: Color(0xff2563eb))),
              const SizedBox(height: 4),
              Text('${product.stockBase.toStringAsFixed(3)} ${product.baseUnit ?? ''}', style: TextStyle(fontSize: 12, color: product.isLow ? const Color(0xffdc2626) : const Color(0xff059669))),
            ]),
          ]),
        ),
      ),
    );
  }
}

class ProductDetailsPage extends StatefulWidget {
  const ProductDetailsPage({super.key, required this.product});
  final ProductSummary product;
  @override
  State<ProductDetailsPage> createState() => _ProductDetailsPageState();
}

class _ProductDetailsPageState extends State<ProductDetailsPage> {
  Map<String, dynamic>? details;
  String? error;
  @override
  void initState() {
    super.initState();
    _load();
  }
  Future<void> _load() async {
    try { details = await context.read<AppState>().productDetails(widget.product.id); }
    catch (e) { error = '$e'; }
    if (mounted) setState(() {});
  }

  @override
  Widget build(BuildContext context) {
    final state = context.read<AppState>();
    return Scaffold(
      appBar: AppBar(title: Text(widget.product.name)),
      floatingActionButton: state.permissions.contains('Inventory.Adjust')
          ? FloatingActionButton.extended(onPressed: () => showStockAdjustment(context, widget.product), icon: const Icon(Icons.tune), label: const Text('تسوية'))
          : null,
      body: details == null
          ? Center(child: error == null ? const CircularProgressIndicator() : ErrorBanner(error!))
          : ListView(padding: const EdgeInsets.all(16), children: [
              if (details!['hasImage'] == true)
                ClipRRect(
                  borderRadius: BorderRadius.circular(18),
                  child: Image.network(state.api.imageUrl(widget.product.id), headers: state.api.imageHeaders, height: 210, fit: BoxFit.cover,
                      errorBuilder: (_, __, ___) => const SizedBox.shrink()),
                ),
              const SizedBox(height: 12),
              InfoCard(children: [
                infoRow('الكود', '${details!['productCode'] ?? '-'}'),
                infoRow('التصنيف', '${details!['category'] ?? '-'}'),
                infoRow('المورد', '${details!['mainSupplier'] ?? '-'}'),
                infoRow('الرصيد الأساسي', '${asDouble(details!['stockBase']).toStringAsFixed(3)} ${details!['baseUnit']}'),
                infoRow('السيريالات المتاحة', '${details!['availableSerials'] ?? 0}'),
              ]),
              const SizedBox(height: 12),
              Text('الأسعار', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
              const SizedBox(height: 8),
              ...(details!['prices'] as List).map((p) => Card(child: ListTile(title: Text('${p['priceGroup']}'), subtitle: Text('أقل سعر ${money(p['minSalePrice'])}'), trailing: Text(money(p['salePrice']), style: const TextStyle(fontWeight: FontWeight.w800))))),
            ]),
    );
  }
}

class CustomersPage extends StatefulWidget {
  const CustomersPage({super.key});
  @override
  State<CustomersPage> createState() => _CustomersPageState();
}

class _CustomersPageState extends State<CustomersPage> {
  final search = TextEditingController();
  @override
  void initState() { super.initState(); Future.microtask(() => context.read<AppState>().searchCustomers('')); }
  @override
  Widget build(BuildContext context) {
    final state = context.watch<AppState>();
    return Column(children: [
      Padding(padding: const EdgeInsets.all(16), child: TextField(controller: search, onSubmitted: state.searchCustomers,
          decoration: InputDecoration(hintText: 'اسم العميل أو الهاتف', prefixIcon: const Icon(Icons.search), suffixIcon: IconButton(onPressed: () => state.searchCustomers(search.text), icon: const Icon(Icons.arrow_forward))))),
      if (state.error != null) Padding(padding: const EdgeInsets.symmetric(horizontal: 16), child: ErrorBanner(state.error!)),
      Expanded(child: ListView.separated(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 20),
        itemCount: state.customers.length,
        separatorBuilder: (_, __) => const SizedBox(height: 9),
        itemBuilder: (_, i) {
          final c = state.customers[i];
          return Card(child: ListTile(
            leading: CircleAvatar(child: Text(c.name.isEmpty ? '?' : c.name[0])),
            title: Text(c.name, style: const TextStyle(fontWeight: FontWeight.w700)),
            subtitle: Text(c.phone ?? 'بدون هاتف'),
            trailing: Text(money(c.balance), style: TextStyle(fontWeight: FontWeight.w800, color: c.balance > 0 ? const Color(0xffdc2626) : const Color(0xff059669))),
            onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => CustomerDetailsPage(customer: c))),
          ));
        },
      )),
    ]);
  }
}

class CustomerDetailsPage extends StatefulWidget {
  const CustomerDetailsPage({super.key, required this.customer});
  final CustomerSummary customer;
  @override
  State<CustomerDetailsPage> createState() => _CustomerDetailsPageState();
}

class _CustomerDetailsPageState extends State<CustomerDetailsPage> {
  Map<String, dynamic>? statement;
  String? error;
  @override
  void initState() { super.initState(); _load(); }
  Future<void> _load() async {
    try { statement = await context.read<AppState>().customerStatement(widget.customer.id); }
    catch (e) { error = '$e'; }
    if (mounted) setState(() {});
  }
  @override
  Widget build(BuildContext context) {
    final state = context.read<AppState>();
    return Scaffold(
      appBar: AppBar(title: Text(widget.customer.name)),
      floatingActionButton: state.permissions.contains('Finance.CustomerCollection')
          ? FloatingActionButton.extended(onPressed: () => showCollection(context, widget.customer, onSaved: _load), icon: const Icon(Icons.payments), label: const Text('تحصيل'))
          : null,
      body: statement == null
          ? Center(child: error == null ? const CircularProgressIndicator() : ErrorBanner(error!))
          : ListView(padding: const EdgeInsets.all(16), children: [
              InfoCard(children: [
                infoRow('الهاتف', '${statement!['customer']['phone'] ?? '-'}'),
                infoRow('الرصيد الحالي', money(statement!['customer']['currentBalance'])),
                infoRow('حد الائتمان', money(statement!['customer']['creditLimit'])),
              ]),
              const SizedBox(height: 14),
              Text('آخر الفواتير', style: Theme.of(context).textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w800)),
              const SizedBox(height: 8),
              ...(statement!['invoices'] as List).take(30).map((i) => Card(child: ListTile(
                    title: Text('${i['invoiceNo']}'),
                    subtitle: Text('${i['invoiceDate']}'.split('T').first),
                    trailing: Column(mainAxisAlignment: MainAxisAlignment.center, crossAxisAlignment: CrossAxisAlignment.end, children: [Text(money(i['totalAmount']), style: const TextStyle(fontWeight: FontWeight.w700)), Text('متبقي ${money(i['remainingAmount'])}', style: const TextStyle(fontSize: 11, color: Color(0xffdc2626)))]),
                  ))),
            ]),
    );
  }
}

class AlertsPage extends StatelessWidget {
  const AlertsPage({super.key});
  @override
  Widget build(BuildContext context) {
    final state = context.watch<AppState>();
    return RefreshIndicator(
      onRefresh: state.refreshHome,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: state.alerts.length,
        separatorBuilder: (_, __) => const SizedBox(height: 9),
        itemBuilder: (_, i) {
          final a = state.alerts[i];
          final critical = a.severity.toLowerCase() == 'critical';
          return Card(child: ListTile(
            leading: CircleAvatar(backgroundColor: critical ? const Color(0xffffe4e6) : const Color(0xfffff7ed), child: Icon(critical ? Icons.error_outline : Icons.warning_amber, color: critical ? const Color(0xffdc2626) : const Color(0xffd97706))),
            title: Text(a.title, style: const TextStyle(fontWeight: FontWeight.w700)),
            subtitle: Text(a.message),
            trailing: a.isRead ? null : IconButton(onPressed: () => state.markAlertRead(a.id), icon: const Icon(Icons.done)),
          ));
        },
      ),
    );
  }
}

class MorePage extends StatelessWidget {
  const MorePage({super.key});
  @override
  Widget build(BuildContext context) {
    final state = context.watch<AppState>();
    return ListView(padding: const EdgeInsets.all(16), children: [
      InfoCard(children: [
        infoRow('عنوان API', state.api.baseUrl),
        infoRow('آخر مزامنة', state.lastSync == null ? 'لم تتم' : DateFormat('dd/MM/yyyy HH:mm').format(state.lastSync!.toLocal())),
        infoRow('عمليات معلقة', '${state.pending.length}'),
        infoRow('الحالة', state.online ? 'متصل' : 'أوفلاين'),
      ]),
      const SizedBox(height: 14),
      FilledButton.icon(
        onPressed: state.busy ? null : () async {
          try { await state.sync(); if (context.mounted) message(context, 'اكتملت المزامنة.'); }
          catch (_) {}
        },
        icon: state.busy ? const SizedBox.square(dimension: 18, child: CircularProgressIndicator(strokeWidth: 2)) : const Icon(Icons.sync),
        label: const Padding(padding: EdgeInsets.symmetric(vertical: 13), child: Text('مزامنة الآن')),
      ),
      if (state.error != null) Padding(padding: const EdgeInsets.only(top: 12), child: ErrorBanner(state.error!)),
      const SizedBox(height: 16),
      Card(child: ListTile(leading: const Icon(Icons.logout, color: Color(0xffdc2626)), title: const Text('تسجيل الخروج'), onTap: state.logout)),
      const Padding(padding: EdgeInsets.all(16), child: Text('Hardware Paint Shop Mobile • RC1', textAlign: TextAlign.center, style: TextStyle(color: Color(0xff94a3b8), fontSize: 12))),
    ]);
  }
}

class AddProductPage extends StatefulWidget {
  const AddProductPage({super.key});
  @override
  State<AddProductPage> createState() => _AddProductPageState();
}

class _AddProductPageState extends State<AddProductPage> {
  final name = TextEditingController();
  final code = TextEditingController();
  final barcode = TextEditingController();
  final price = TextEditingController();
  final minimum = TextEditingController(text: '0');
  String? unitId;
  String? categoryId;
  String? priceGroupId;
  XFile? image;
  bool serial = false;
  bool saving = false;

  @override
  void initState() {
    super.initState();
    Future.microtask(() async {
      try { await context.read<AppState>().loadLookups(); }
      catch (_) {}
      if (mounted) setState(() {});
    });
  }

  @override
  Widget build(BuildContext context) {
    final state = context.watch<AppState>();
    final units = (state.lookups['units'] as List?) ?? [];
    final categories = (state.lookups['categories'] as List?) ?? [];
    final groups = (state.lookups['priceGroups'] as List?) ?? [];
    unitId ??= units.isEmpty ? null : '${units.first['id']}';
    priceGroupId ??= groups.isEmpty ? null : '${groups.first['id']}';
    return Scaffold(
      appBar: AppBar(title: const Text('إضافة منتج')),
      body: ListView(padding: const EdgeInsets.all(16), children: [
        TextField(controller: name, decoration: const InputDecoration(labelText: 'اسم المنتج *')),
        const SizedBox(height: 10),
        TextField(controller: code, decoration: const InputDecoration(labelText: 'الكود الداخلي')),
        const SizedBox(height: 10),
        TextField(controller: barcode, textDirection: TextDirection.ltr, decoration: const InputDecoration(labelText: 'الباركود')),
        const SizedBox(height: 10),
        DropdownButtonFormField<String>(value: unitId, decoration: const InputDecoration(labelText: 'الوحدة الأساسية *'), items: units.map<DropdownMenuItem<String>>((u) => DropdownMenuItem(value: '${u['id']}', child: Text('${u['name']}'))).toList(), onChanged: (v) => setState(() => unitId = v)),
        const SizedBox(height: 10),
        DropdownButtonFormField<String>(value: categoryId, decoration: const InputDecoration(labelText: 'التصنيف'), items: categories.map<DropdownMenuItem<String>>((c) => DropdownMenuItem(value: '${c['id']}', child: Text('${c['name']}'))).toList(), onChanged: (v) => setState(() => categoryId = v)),
        const SizedBox(height: 10),
        DropdownButtonFormField<String>(value: priceGroupId, decoration: const InputDecoration(labelText: 'فئة السعر'), items: groups.map<DropdownMenuItem<String>>((g) => DropdownMenuItem(value: '${g['id']}', child: Text('${g['name']}'))).toList(), onChanged: (v) => setState(() => priceGroupId = v)),
        const SizedBox(height: 10),
        Row(children: [Expanded(child: TextField(controller: price, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'سعر البيع'))), const SizedBox(width: 10), Expanded(child: TextField(controller: minimum, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'حد المخزون')))]),
        SwitchListTile(value: serial, onChanged: (v) => setState(() => serial = v), title: const Text('المنتج له سيريال نمبر')),
        OutlinedButton.icon(onPressed: () async {
          final picked = await ImagePicker().pickImage(source: ImageSource.camera, imageQuality: 78, maxWidth: 1600);
          if (picked != null) setState(() => image = picked);
        }, icon: const Icon(Icons.camera_alt_outlined), label: Text(image == null ? 'تصوير المنتج' : 'تم اختيار الصورة')),
        const SizedBox(height: 18),
        FilledButton(onPressed: saving || unitId == null ? null : () async {
          setState(() => saving = true);
          try {
            String? encoded;
            if (image != null) encoded = base64Encode(await File(image!.path).readAsBytes());
            await state.createProduct({
              'name': name.text, 'productCode': code.text, 'baseUnitId': unitId,
              'categoryId': categoryId, 'mainSupplierId': null,
              'minStockBaseQuantity': double.tryParse(minimum.text) ?? 0,
              'isSerialTracked': serial, 'barcode': barcode.text,
              'priceGroupId': priceGroupId, 'salePrice': double.tryParse(price.text),
              'minSalePrice': 0, 'imageBase64': encoded, 'notes': 'أضيف من الموبايل',
            });
            if (context.mounted) { message(context, state.online ? 'تم حفظ المنتج.' : 'تم حفظه للمزامنة عند الاتصال.'); Navigator.pop(context); }
          } catch (e) { if (context.mounted) message(context, '$e', error: true); }
          finally { if (mounted) setState(() => saving = false); }
        }, child: Padding(padding: const EdgeInsets.symmetric(vertical: 13), child: Text(saving ? 'جارٍ الحفظ...' : 'حفظ المنتج'))),
      ]),
    );
  }
}

class ScannerPage extends StatefulWidget {
  const ScannerPage({super.key});
  @override
  State<ScannerPage> createState() => _ScannerPageState();
}

class _ScannerPageState extends State<ScannerPage> {
  bool returned = false;
  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('مسح الباركود')),
    body: Stack(children: [
      MobileScanner(onDetect: (capture) {
        if (returned || capture.barcodes.isEmpty) return;
        final value = capture.barcodes.first.rawValue;
        if (value != null && value.isNotEmpty) { returned = true; Navigator.pop(context, value); }
      }),
      Center(child: Container(width: 260, height: 170, decoration: BoxDecoration(border: Border.all(color: Colors.white, width: 3), borderRadius: BorderRadius.circular(18)))),
    ]),
  );
}

Future<void> showCollection(BuildContext context, CustomerSummary customer, {required VoidCallback onSaved}) async {
  final state = context.read<AppState>();
  try { await state.loadLookups(); } catch (_) {}
  if (!context.mounted) return;
  final cashboxes = (state.lookups['cashboxes'] as List?) ?? [];
  String? cashboxId = cashboxes.isEmpty ? null : '${cashboxes.first['id']}';
  final amount = TextEditingController();
  final notes = TextEditingController();
  await showDialog(context: context, builder: (dialogContext) => StatefulBuilder(builder: (context, setDialogState) => AlertDialog(
    title: Text('تحصيل من ${customer.name}'),
    content: Column(mainAxisSize: MainAxisSize.min, children: [
      Text('الرصيد الحالي: ${money(customer.balance)}'), const SizedBox(height: 12),
      TextField(controller: amount, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'المبلغ')),
      const SizedBox(height: 10),
      DropdownButtonFormField<String>(value: cashboxId, decoration: const InputDecoration(labelText: 'الخزينة'), items: cashboxes.map<DropdownMenuItem<String>>((c) => DropdownMenuItem(value: '${c['id']}', child: Text('${c['name']}'))).toList(), onChanged: (v) => setDialogState(() => cashboxId = v)),
      const SizedBox(height: 10), TextField(controller: notes, decoration: const InputDecoration(labelText: 'ملاحظات')),
    ]),
    actions: [TextButton(onPressed: () => Navigator.pop(dialogContext), child: const Text('إلغاء')), FilledButton(onPressed: cashboxId == null ? null : () async {
      try {
        await state.collectCustomer(customer.id, double.tryParse(amount.text) ?? 0, cashboxId!, notes.text);
        if (dialogContext.mounted) Navigator.pop(dialogContext);
        onSaved();
      } catch (e) { if (dialogContext.mounted) message(dialogContext, '$e', error: true); }
    }, child: const Text('حفظ'))],
  )));
}

Future<void> showStockAdjustment(BuildContext context, ProductSummary product) async {
  final difference = TextEditingController();
  final reason = TextEditingController();
  final state = context.read<AppState>();
  await showDialog(context: context, builder: (dialogContext) => AlertDialog(
    title: Text('تسوية ${product.name}'),
    content: Column(mainAxisSize: MainAxisSize.min, children: [
      Text('الرصيد الحالي: ${product.stockBase.toStringAsFixed(3)}'), const SizedBox(height: 12),
      TextField(controller: difference, keyboardType: const TextInputType.numberWithOptions(signed: true, decimal: true), decoration: const InputDecoration(labelText: 'الفرق (+ زيادة / - نقص)')),
      const SizedBox(height: 10), TextField(controller: reason, decoration: const InputDecoration(labelText: 'السبب *')),
    ]),
    actions: [TextButton(onPressed: () => Navigator.pop(dialogContext), child: const Text('إلغاء')), FilledButton(onPressed: () async {
      try {
        await state.adjustStock(product.id, double.tryParse(difference.text) ?? 0, reason.text);
        if (dialogContext.mounted) Navigator.pop(dialogContext);
      } catch (e) { if (dialogContext.mounted) message(dialogContext, '$e', error: true); }
    }, child: const Text('حفظ'))],
  ));
}

class MetricCard extends StatelessWidget {
  const MetricCard(this.label, this.value, this.icon, this.color, {super.key});
  final String label;
  final String value;
  final IconData icon;
  final Color color;
  @override
  Widget build(BuildContext context) => Card(child: Padding(padding: const EdgeInsets.all(14), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
    CircleAvatar(radius: 18, backgroundColor: color.withOpacity(.12), child: Icon(icon, color: color, size: 20)),
    const Spacer(), Text(value, maxLines: 1, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
    Text(label, style: const TextStyle(fontSize: 12, color: Color(0xff64748b))),
  ])));
}

class InfoCard extends StatelessWidget {
  const InfoCard({super.key, required this.children});
  final List<Widget> children;
  @override
  Widget build(BuildContext context) => Card(child: Padding(padding: const EdgeInsets.all(16), child: Column(children: children)));
}

Widget infoRow(String label, String value) => Padding(
  padding: const EdgeInsets.symmetric(vertical: 6),
  child: Row(children: [Expanded(child: Text(label, style: const TextStyle(color: Color(0xff64748b)))), Flexible(child: Text(value, textAlign: TextAlign.left, style: const TextStyle(fontWeight: FontWeight.w700)))]),
);

class ErrorBanner extends StatelessWidget {
  const ErrorBanner(this.message, {super.key});
  final String message;
  @override
  Widget build(BuildContext context) => Container(width: double.infinity, padding: const EdgeInsets.all(12), decoration: BoxDecoration(color: const Color(0xfffff1f2), borderRadius: BorderRadius.circular(12)), child: Text(message, style: const TextStyle(color: Color(0xffbe123c))));
}

String money(dynamic value) => NumberFormat.currency(locale: 'ar_EG', symbol: 'ج.م', decimalDigits: 2).format(asDouble(value));

void message(BuildContext context, String text, {bool error = false}) {
  ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(text), backgroundColor: error ? const Color(0xffbe123c) : const Color(0xff059669)));
}
