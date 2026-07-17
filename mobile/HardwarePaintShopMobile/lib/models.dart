import 'dart:convert';

double asDouble(dynamic value) => value is num ? value.toDouble() : double.tryParse('$value') ?? 0;

class ProductSummary {
  const ProductSummary({
    required this.id,
    required this.name,
    required this.stockBase,
    this.code,
    this.category,
    this.baseUnit,
    this.defaultPrice,
    this.minimum = 0,
    this.serialTracked = false,
    this.hasImage = false,
    this.updatedAt,
  });

  final String id;
  final String name;
  final String? code;
  final String? category;
  final String? baseUnit;
  final double stockBase;
  final double? defaultPrice;
  final double minimum;
  final bool serialTracked;
  final bool hasImage;
  final DateTime? updatedAt;

  bool get isLow => stockBase <= minimum;

  factory ProductSummary.fromJson(Map<String, dynamic> json) => ProductSummary(
        id: '${json['id']}',
        name: '${json['name'] ?? ''}',
        code: json['productCode']?.toString(),
        category: json['category']?.toString(),
        baseUnit: json['baseUnit']?.toString(),
        stockBase: asDouble(json['stockBase']),
        defaultPrice: json['defaultPrice'] == null ? null : asDouble(json['defaultPrice']),
        minimum: asDouble(json['minStockBaseQuantity']),
        serialTracked: json['isSerialTracked'] == true,
        hasImage: json['hasImage'] == true,
        updatedAt: DateTime.tryParse('${json['updatedAt'] ?? ''}'),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'productCode': code,
        'category': category,
        'baseUnit': baseUnit,
        'stockBase': stockBase,
        'defaultPrice': defaultPrice,
        'minStockBaseQuantity': minimum,
        'isSerialTracked': serialTracked,
        'hasImage': hasImage,
        'updatedAt': updatedAt?.toIso8601String(),
      };
}

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

  factory CustomerSummary.fromJson(Map<String, dynamic> json) => CustomerSummary(
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

class ShopAlert {
  const ShopAlert({
    required this.id,
    required this.title,
    required this.message,
    required this.severity,
    required this.isRead,
    required this.createdAt,
  });

  final String id;
  final String title;
  final String message;
  final String severity;
  final bool isRead;
  final DateTime createdAt;

  factory ShopAlert.fromJson(Map<String, dynamic> json) => ShopAlert(
        id: '${json['id']}',
        title: '${json['title'] ?? ''}',
        message: '${json['message'] ?? ''}',
        severity: '${json['severity'] ?? 'Info'}',
        isRead: json['isRead'] == true,
        createdAt: DateTime.tryParse('${json['createdAt'] ?? ''}') ?? DateTime.now(),
      );

  Map<String, dynamic> toJson() => {
        'id': id,
        'title': title,
        'message': message,
        'severity': severity,
        'isRead': isRead,
        'createdAt': createdAt.toIso8601String(),
      };
}

class PendingOperation {
  const PendingOperation({
    required this.operationId,
    required this.type,
    required this.payload,
    required this.createdAt,
    this.status = 'pending',
    this.error,
  });

  final String operationId;
  final String type;
  final Map<String, dynamic> payload;
  final DateTime createdAt;
  final String status;
  final String? error;

  Map<String, dynamic> toApiJson() => {
        'operationId': operationId,
        'type': type,
        'payload': payload,
        'createdAt': createdAt.toUtc().toIso8601String(),
      };

  Map<String, dynamic> toDbJson() => {
        'operation_id': operationId,
        'type': type,
        'payload': jsonEncode(payload),
        'created_at': createdAt.toUtc().toIso8601String(),
        'status': status,
        'error': error,
      };

  factory PendingOperation.fromDb(Map<String, dynamic> row) => PendingOperation(
        operationId: '${row['operation_id']}',
        type: '${row['type']}',
        payload: jsonDecode('${row['payload']}') as Map<String, dynamic>,
        createdAt: DateTime.parse('${row['created_at']}'),
        status: '${row['status']}',
        error: row['error']?.toString(),
      );
}
