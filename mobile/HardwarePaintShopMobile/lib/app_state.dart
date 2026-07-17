import 'dart:convert';
import 'dart:math';

import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'api_client.dart';
import 'local_store.dart';
import 'models.dart';

class AppState extends ChangeNotifier {
  AppState(this.api, this.store);

  final ApiClient api;
  final LocalStore store;

  bool initialized = false;
  bool busy = false;
  bool authenticated = false;
  bool online = false;
  String? error;
  String userName = '';
  String shopName = 'محل الحدايد والبوهيات';
  Set<String> permissions = {};
  Map<String, dynamic> dashboard = {};
  List<ProductSummary> products = [];
  List<CustomerSummary> customers = [];
  List<ShopAlert> alerts = [];
  List<PendingOperation> pending = [];
  Map<String, dynamic> lookups = {};
  DateTime? lastSync;

  Future<void> initialize() async {
    await api.initialize();
    lastSync = await store.lastSync();
    pending = await store.pendingOperations();
    if (api.hasToken) {
      try {
        final me = await api.get('/api/auth/me') as Map<String, dynamic>;
        _applyUser(me);
        authenticated = true;
        online = true;
        await refreshHome();
      } catch (_) {
        authenticated = false;
      }
    }
    initialized = true;
    notifyListeners();
  }

  Future<void> login(String url, String username, String password) async {
    await _work(() async {
      await api.configure(url);
      final result = await api.login(username.trim(), password, await _deviceName());
      userName = '${result['userName'] ?? ''}';
      permissions = ((result['permissions'] as List?) ?? []).map((e) => '$e').toSet();
      authenticated = true;
      online = true;
      await refreshHome();
    });
  }

  Future<void> logout() async {
    await api.logout();
    authenticated = false;
    userName = '';
    permissions = {};
    dashboard = {};
    notifyListeners();
  }

  Future<void> refreshHome() async {
    await _work(() async {
      final values = await Future.wait([
        api.get('/api/dashboard'),
        api.get('/api/settings/mobile'),
        api.get('/api/alerts', query: {'unreadOnly': 'true'}),
      ]);
      dashboard = values[0] as Map<String, dynamic>;
      final settings = values[1] as Map<String, dynamic>;
      shopName = '${settings['shopName'] ?? shopName}';
      final alertRows = (values[2] as List).cast<Map<String, dynamic>>();
      alerts = alertRows.map(ShopAlert.fromJson).toList();
      await store.saveAlerts(alertRows);
      online = true;
    }, useBusy: false);
  }

  Future<void> searchProducts(String query) async {
    error = null;
    try {
      final rows = (await api.get('/api/products/search', query: {'query': query, 'limit': '100'}) as List)
          .cast<Map<String, dynamic>>();
      products = rows.map(ProductSummary.fromJson).toList();
      await store.saveProducts(rows);
      online = true;
    } catch (e) {
      products = await store.searchProducts(query);
      online = false;
      error = products.isEmpty ? '$e' : 'عرض نتائج محفوظة لأن اللاب غير متصل.';
    }
    notifyListeners();
  }

  Future<Map<String, dynamic>> productDetails(String id) async {
    final value = await api.get('/api/products/$id');
    online = true;
    return value as Map<String, dynamic>;
  }

  Future<void> scanLookup(String value, {bool serial = false}) async {
    await _work(() async {
      final path = serial ? '/api/products/serial/${Uri.encodeComponent(value)}' : '/api/products/barcode/${Uri.encodeComponent(value)}';
      final row = await api.get(path) as Map<String, dynamic>;
      products = [ProductSummary.fromJson(row)];
      online = true;
    });
  }

  Future<void> searchCustomers(String query) async {
    error = null;
    try {
      final rows = (await api.get('/api/customers/search', query: {'query': query, 'limit': '150'}) as List)
          .cast<Map<String, dynamic>>();
      customers = rows.map(CustomerSummary.fromJson).toList();
      await store.saveCustomers(rows);
      online = true;
    } catch (e) {
      customers = await store.searchCustomers(query);
      online = false;
      error = customers.isEmpty ? '$e' : 'عرض عملاء محفوظين لأن اللاب غير متصل.';
    }
    notifyListeners();
  }

  Future<Map<String, dynamic>> customerStatement(String id) async {
    final value = await api.get('/api/customers/$id/statement');
    online = true;
    return value as Map<String, dynamic>;
  }

  Future<void> collectCustomer(String customerId, double amount, String cashboxId, String notes) async {
    final payload = {
      'customerId': customerId,
      'amount': amount,
      'cashboxId': cashboxId,
      'operationDate': DateTime.now().toUtc().toIso8601String(),
      'notes': notes,
    };
    await _sendOrQueue('customer_payment', payload,
        () => api.post('/api/customers/$customerId/payments', body: Map.of(payload)..remove('customerId')));
  }

  Future<void> adjustStock(String productId, double difference, String reason) async {
    final payload = {
      'productId': productId,
      'quantityBase': difference,
      'reason': reason,
      'operationDate': DateTime.now().toUtc().toIso8601String(),
    };
    await _sendOrQueue('stock_adjustment', payload, () => api.post('/api/stock/adjustment', body: payload));
  }

  Future<void> createProduct(Map<String, dynamic> payload) async {
    await _sendOrQueue('product_create', payload, () => api.post('/api/products', body: payload));
  }

  Future<void> loadLookups() async {
    if (lookups.isNotEmpty) return;
    lookups = await api.get('/api/lookups') as Map<String, dynamic>;
    notifyListeners();
  }

  Future<void> sync() async {
    await _work(() async {
      pending = await store.pendingOperations();
      if (pending.isNotEmpty) {
        final response = await api.post('/api/sync/upload', body: {
          'operations': pending.map((e) => e.toApiJson()).toList(),
        }) as Map<String, dynamic>;
        for (final row in (response['results'] as List).cast<Map<String, dynamic>>()) {
          await store.markOperation('${row['operationId']}', '${row['status']}', error: row['message']?.toString());
        }
        await store.removeSuccessfulOperations();
      }
      final since = lastSync ?? DateTime.now().toUtc().subtract(const Duration(days: 30));
      final snapshot = await api.get('/api/sync/download', query: {'since': since.toIso8601String()}) as Map<String, dynamic>;
      final productRows = (snapshot['products'] as List).cast<Map<String, dynamic>>();
      final customerRows = (snapshot['customers'] as List).cast<Map<String, dynamic>>();
      final alertRows = (snapshot['alerts'] as List).cast<Map<String, dynamic>>();
      final barcodeRows = (snapshot['barcodes'] as List).cast<Map<String, dynamic>>();
      await store.saveProducts(productRows);
      await store.saveCustomers(customerRows);
      await store.saveAlerts(alertRows);
      await store.saveBarcodes(barcodeRows);
      lastSync = DateTime.parse('${snapshot['serverTime']}');
      await store.setLastSync(lastSync!);
      pending = await store.pendingOperations();
      online = true;
    });
  }

  Future<void> markAlertRead(String id) async {
    await api.post('/api/alerts/$id/read');
    await refreshHome();
  }

  Future<void> _sendOrQueue(String type, Map<String, dynamic> payload, Future<dynamic> Function() onlineAction) async {
    try {
      await onlineAction();
      online = true;
    } catch (e) {
      if (e is ApiException && e.statusCode != null) rethrow;
      final operation = PendingOperation(
        operationId: _uuid(),
        type: type,
        payload: payload,
        createdAt: DateTime.now().toUtc(),
      );
      await store.queue(operation);
      pending = await store.pendingOperations();
      online = false;
    }
    notifyListeners();
  }

  Future<void> _work(Future<void> Function() action, {bool useBusy = true}) async {
    if (useBusy) busy = true;
    error = null;
    notifyListeners();
    try {
      await action();
    } catch (e) {
      error = '$e';
      rethrow;
    } finally {
      if (useBusy) busy = false;
      notifyListeners();
    }
  }

  void _applyUser(Map<String, dynamic> row) {
    userName = '${row['userName'] ?? ''}';
    permissions = ((row['permissions'] as List?) ?? []).map((e) => '$e').toSet();
  }

  Future<String> _deviceName() async {
    final prefs = await SharedPreferences.getInstance();
    var value = prefs.getString('device_name');
    if (value == null) {
      value = 'Android-${_uuid().substring(0, 8)}';
      await prefs.setString('device_name', value);
    }
    return value;
  }

  String _uuid() {
    final random = Random.secure();
    final bytes = List<int>.generate(16, (_) => random.nextInt(256));
    bytes[6] = (bytes[6] & 0x0f) | 0x40;
    bytes[8] = (bytes[8] & 0x3f) | 0x80;
    final hex = bytes.map((b) => b.toRadixString(16).padLeft(2, '0')).join();
    return '${hex.substring(0, 8)}-${hex.substring(8, 12)}-${hex.substring(12, 16)}-'
        '${hex.substring(16, 20)}-${hex.substring(20)}';
  }
}
