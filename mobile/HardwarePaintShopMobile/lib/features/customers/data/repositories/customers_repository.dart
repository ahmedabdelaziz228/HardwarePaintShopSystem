import 'package:hardware_paint_shop_mobile/core/errors/api_exception.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/network/api_client.dart';
import 'package:hardware_paint_shop_mobile/core/storage/app_database.dart';
import 'package:hardware_paint_shop_mobile/core/storage/offline_operation_queue.dart';
import 'package:hardware_paint_shop_mobile/features/customers/data/models/cashbox_option.dart';
import 'package:hardware_paint_shop_mobile/features/customers/data/models/customer_statement.dart';
import 'package:hardware_paint_shop_mobile/features/customers/data/models/customer_summary.dart';

class CustomerSearchResult {
  const CustomerSearchResult({
    required this.customers,
    required this.online,
    this.warning,
  });

  final List<CustomerSummary> customers;
  final bool online;
  final String? warning;
}

/// Customer search, statement, and collection data boundary.
class CustomersRepository {
  CustomersRepository(
    this._apiClient,
    this._database,
    this._operationQueue,
  );

  final ApiClient _apiClient;
  final AppDatabase _database;
  final OfflineOperationQueue _operationQueue;

  Future<CustomerSearchResult> search(String query) async {
    try {
      final rows = await _apiClient.get<List<dynamic>>(
        '/api/customers/search',
        query: {'query': query, 'limit': 150},
      );
      final jsonRows = rows
          .map((row) => Map<String, dynamic>.from(row as Map))
          .toList();
      await _database.saveCustomers(jsonRows);
      return CustomerSearchResult(
        customers: jsonRows.map(CustomerSummary.fromJson).toList(),
        online: true,
      );
    } on NetworkException catch (error) {
      final cachedRows = await _database.searchCustomerRows(query);
      if (cachedRows.isEmpty) rethrow;
      return CustomerSearchResult(
        customers: cachedRows.map(CustomerSummary.fromJson).toList(),
        online: false,
        warning: '${error.message} عرض عملاء محفوظين.',
      );
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<CustomerStatement> statement(String customerId) async {
    try {
      final json = await _apiClient.get<Map<String, dynamic>>(
        '/api/customers/$customerId/statement',
      );
      return CustomerStatement.fromJson(json);
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<List<CashboxOption>> cashboxes() async {
    try {
      final json = await _apiClient.get<Map<String, dynamic>>('/api/lookups');
      return ((json['cashboxes'] as List?) ?? const [])
          .map((row) => CashboxOption.fromJson(
                Map<String, dynamic>.from(row as Map),
              ))
          .toList();
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<bool> collect({
    required String customerId,
    required double amount,
    required String cashboxId,
    required String notes,
  }) async {
    final payload = {
      'customerId': customerId,
      'amount': amount,
      'cashboxId': cashboxId,
      'operationDate': DateTime.now().toUtc().toIso8601String(),
      'notes': notes,
    };
    try {
      return _operationQueue.sendOrQueue(
        type: 'customer_payment',
        payload: payload,
        onlineAction: () async {
          await _apiClient.post<dynamic>(
            '/api/customers/$customerId/payments',
            body: Map<String, dynamic>.of(payload)..remove('customerId'),
          );
        },
      );
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }
}
