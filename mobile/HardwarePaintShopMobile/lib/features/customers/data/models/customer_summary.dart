import 'package:hardware_paint_shop_mobile/core/utils/json_parsing.dart';

class CustomerSummary {
  const CustomerSummary({
    required this.id,
    required this.name,
    required this.balance,
    this.phone,
    this.address,
    this.creditLimit = 0,
    this.updatedAt,
  });

  final String id;
  final String name;
  final String? phone;
  final String? address;
  final double balance;
  final double creditLimit;
  final DateTime? updatedAt;

  factory CustomerSummary.fromJson(Map<String, dynamic> json) =>
      CustomerSummary(
        id: '${json['id']}',
        name: '${json['name'] ?? ''}',
        phone: json['phone']?.toString(),
        address: json['address']?.toString(),
        balance: asDouble(json['currentBalance']),
        creditLimit: asDouble(json['creditLimit']),
        updatedAt: DateTime.tryParse('${json['updatedAt'] ?? ''}'),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'phone': phone,
        'address': address,
        'currentBalance': balance,
        'creditLimit': creditLimit,
        'updatedAt': updatedAt?.toIso8601String(),
      };
}
