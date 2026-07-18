import 'package:hardware_paint_shop_mobile/core/utils/json_parsing.dart';

class DashboardSummary {
  const DashboardSummary({
    this.salesToday = 0,
    this.collectedToday = 0,
    this.expensesToday = 0,
    this.cashBalance = 0,
    this.customerDebt = 0,
    this.lowStockCount = 0,
  });

  final double salesToday;
  final double collectedToday;
  final double expensesToday;
  final double cashBalance;
  final double customerDebt;
  final int lowStockCount;

  factory DashboardSummary.fromJson(Map<String, dynamic> json) =>
      DashboardSummary(
        salesToday: asDouble(json['salesToday']),
        collectedToday: asDouble(json['collectedToday']),
        expensesToday: asDouble(json['expensesToday']),
        cashBalance: asDouble(json['cashBalance']),
        customerDebt: asDouble(json['customerDebt']),
        lowStockCount: int.tryParse('${json['lowStockCount'] ?? 0}') ?? 0,
      );
}
