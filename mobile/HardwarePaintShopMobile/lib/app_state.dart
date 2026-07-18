import 'package:flutter/foundation.dart';
import 'package:hardware_paint_shop_mobile/core/errors/api_exception.dart';
import 'package:hardware_paint_shop_mobile/core/network/api_client.dart';
import 'package:hardware_paint_shop_mobile/core/utils/id_generator.dart';
import 'package:hardware_paint_shop_mobile/data/local/app_database.dart';

import 'models.dart';

/// Temporary coordinator for non-authenticated RC1 features during migration.
class AppState extends ChangeNotifier {
  AppState(this.api, this.database);

  final ApiClient api;
  final AppDatabase database;

  bool initialized = false;
  bool busy = false;
  bool online = false;
  String? error;
  String shopName = 'محل الحدايد والبوهيات';
  Map<String, dynamic> dashboard = {};
  List<ProductSummary> products = [];
  List<CustomerSummary> customers = [];
  List<ShopAlert> alerts = [];
  List<PendingOperation> pending = [];
  Map<String, dynamic> lookups = {};
  DateTime? lastSync;

  Future<void> initializeLocalState() async {
    lastSync = await database.lastSync();
    pending = await database.pendingOperations();
    initialized = true;
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
      await database.saveAlerts(alertRows);
      online = true;
    }, useBusy: false);
  }

  Future<void> searchProducts(String query) async {
    error = null;
    try {
      final rows = (await api.get('/api/products/search', query: {'query': query, 'limit': '100'}) as List)
          .cast<Map<String, dynamic>>();
      products = rows.map(ProductSummary.fromJson).toList();
      await database.saveProducts(rows);
      online = true;
    } catch (e) {
      products = await database.searchProducts(query);
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
      await database.saveCustomers(rows);
      online = true;
    } catch (e) {
      customers = await database.searchCustomers(query);
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
      pending = await database.pendingOperations();
      if (pending.isNotEmpty) {
        final response = await api.post('/api/sync/upload', body: {
          'operations': pending.map((e) => e.toApiJson()).toList(),
        }) as Map<String, dynamic>;
        for (final row in (response['results'] as List).cast<Map<String, dynamic>>()) {
          await database.markOperation('${row['operationId']}', '${row['status']}', error: row['message']?.toString());
        }
        await database.removeSuccessfulOperations();
      }
      final since = lastSync ?? DateTime.now().toUtc().subtract(const Duration(days: 30));
      final snapshot = await api.get('/api/sync/download', query: {'since': since.toIso8601String()}) as Map<String, dynamic>;
      final productRows = (snapshot['products'] as List).cast<Map<String, dynamic>>();
      final customerRows = (snapshot['customers'] as List).cast<Map<String, dynamic>>();
      final alertRows = (snapshot['alerts'] as List).cast<Map<String, dynamic>>();
      final barcodeRows = (snapshot['barcodes'] as List).cast<Map<String, dynamic>>();
      await database.saveProducts(productRows);
      await database.saveCustomers(customerRows);
      await database.saveAlerts(alertRows);
      await database.saveBarcodes(barcodeRows);
      lastSync = DateTime.parse('${snapshot['serverTime']}');
      await database.setLastSync(lastSync!);
      pending = await database.pendingOperations();
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
        operationId: IdGenerator.uuidV4(),
        type: type,
        payload: payload,
        createdAt: DateTime.now().toUtc(),
      );
      await database.queue(operation);
      pending = await database.pendingOperations();
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

}
