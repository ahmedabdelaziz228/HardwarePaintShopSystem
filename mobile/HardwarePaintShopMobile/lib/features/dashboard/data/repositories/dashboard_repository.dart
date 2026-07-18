import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/network/api_client.dart';
import 'package:hardware_paint_shop_mobile/features/dashboard/data/models/dashboard_summary.dart';

class DashboardPayload {
  const DashboardPayload({required this.summary, required this.shopName});

  final DashboardSummary summary;
  final String shopName;
}

/// Loads the dashboard and shop identity through the shared Dio client.
class DashboardRepository {
  DashboardRepository(this._apiClient);

  final ApiClient _apiClient;

  Future<DashboardPayload> load() async {
    try {
      final values = await Future.wait<dynamic>([
        _apiClient.get<Map<String, dynamic>>('/api/dashboard'),
        _apiClient.get<Map<String, dynamic>>('/api/settings/mobile'),
      ]);
      final dashboard = values[0] as Map<String, dynamic>;
      final settings = values[1] as Map<String, dynamic>;
      return DashboardPayload(
        summary: DashboardSummary.fromJson(dashboard),
        shopName: '${settings['shopName'] ?? 'محل الحدايد والبوهيات'}',
      );
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }
}
