import 'package:hardware_paint_shop_mobile/core/errors/api_exception.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/network/api_client.dart';
import 'package:hardware_paint_shop_mobile/core/storage/app_database.dart';
import 'package:hardware_paint_shop_mobile/features/alerts/data/models/shop_alert.dart';

class AlertsResult {
  const AlertsResult({
    required this.alerts,
    required this.online,
    this.warning,
  });

  final List<ShopAlert> alerts;
  final bool online;
  final String? warning;
}

class AlertsRepository {
  AlertsRepository(this._apiClient, this._database);

  final ApiClient _apiClient;
  final AppDatabase _database;

  Future<AlertsResult> load() async {
    try {
      final rows = await _apiClient.get<List<dynamic>>(
        '/api/alerts',
        query: {'unreadOnly': true},
      );
      final jsonRows = rows
          .map((row) => Map<String, dynamic>.from(row as Map))
          .toList();
      await _database.saveAlerts(jsonRows);
      return AlertsResult(
        alerts: jsonRows.map(ShopAlert.fromJson).toList(),
        online: true,
      );
    } on NetworkException catch (error) {
      final cachedRows = await _database.alertRows();
      return AlertsResult(
        alerts: cachedRows.map(ShopAlert.fromJson).toList(),
        online: false,
        warning: '${error.message} عرض تنبيهات محفوظة.',
      );
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<void> markRead(String alertId) async {
    try {
      await _apiClient.post<dynamic>('/api/alerts/$alertId/read');
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }
}
