import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/network/api_client.dart';
import 'package:hardware_paint_shop_mobile/core/storage/app_database.dart';
import 'package:hardware_paint_shop_mobile/features/sync/data/models/pending_operation.dart';

class SyncSnapshot {
  const SyncSnapshot({
    required this.pending,
    required this.lastSync,
  });

  final List<PendingOperation> pending;
  final DateTime? lastSync;
}

/// Owns bidirectional sync and local pending-operation bookkeeping.
class SyncRepository {
  SyncRepository(this._apiClient, this._database);

  final ApiClient _apiClient;
  final AppDatabase _database;

  String get baseUrl => _apiClient.baseUrl;

  Future<SyncSnapshot> localSnapshot() async {
    try {
      return SyncSnapshot(
        pending: await _pending(),
        lastSync: await _database.lastSync(),
      );
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<SyncSnapshot> synchronize(DateTime? previousSync) async {
    try {
      final pending = await _pending();
      if (pending.isNotEmpty) {
        final response = await _apiClient.post<Map<String, dynamic>>(
          '/api/sync/upload',
          body: {
            'operations': pending.map((item) => item.toApiJson()).toList(),
          },
        );
        for (final row in ((response['results'] as List?) ?? const [])) {
          final result = Map<String, dynamic>.from(row as Map);
          await _database.markOperation(
            '${result['operationId']}',
            '${result['status']}',
            error: result['message']?.toString(),
          );
        }
        await _database.removeSuccessfulOperations();
      }

      final since = previousSync ??
          DateTime.now().toUtc().subtract(const Duration(days: 30));
      final snapshot = await _apiClient.get<Map<String, dynamic>>(
        '/api/sync/download',
        query: {'since': since.toIso8601String()},
      );
      await _database.saveProducts(_rows(snapshot['products']));
      await _database.saveCustomers(_rows(snapshot['customers']));
      await _database.saveAlerts(_rows(snapshot['alerts']));
      await _database.saveBarcodes(_rows(snapshot['barcodes']));
      final lastSync = DateTime.parse('${snapshot['serverTime']}');
      await _database.setLastSync(lastSync);
      return SyncSnapshot(pending: await _pending(), lastSync: lastSync);
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<List<PendingOperation>> _pending() async {
    try {
      final rows = await _database.pendingOperationRows();
      return rows.map(PendingOperation.fromDb).toList();
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  static List<Map<String, dynamic>> _rows(dynamic value) =>
      ((value as List?) ?? const [])
          .map((row) => Map<String, dynamic>.from(row as Map))
          .toList();
}
