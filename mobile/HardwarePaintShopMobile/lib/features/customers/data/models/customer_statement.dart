import 'package:hardware_paint_shop_mobile/core/utils/json_parsing.dart';

class CustomerInvoiceSummary {
  const CustomerInvoiceSummary({
    required this.invoiceNumber,
    required this.invoiceDate,
    required this.totalAmount,
    required this.remainingAmount,
  });

  final String invoiceNumber;
  final DateTime? invoiceDate;
  final double totalAmount;
  final double remainingAmount;

  factory CustomerInvoiceSummary.fromJson(Map<String, dynamic> json) =>
      CustomerInvoiceSummary(
        invoiceNumber: '${json['invoiceNo'] ?? ''}',
        invoiceDate: DateTime.tryParse('${json['invoiceDate'] ?? ''}'),
        totalAmount: asDouble(json['totalAmount']),
        remainingAmount: asDouble(json['remainingAmount']),
      );
}

class CustomerStatement {
  const CustomerStatement({
    required this.phone,
    required this.currentBalance,
    required this.creditLimit,
    required this.invoices,
  });

  final String? phone;
  final double currentBalance;
  final double creditLimit;
  final List<CustomerInvoiceSummary> invoices;

  factory CustomerStatement.fromJson(Map<String, dynamic> json) {
    final customer = Map<String, dynamic>.from((json['customer'] as Map?) ?? {});
    return CustomerStatement(
      phone: customer['phone']?.toString(),
      currentBalance: asDouble(customer['currentBalance']),
      creditLimit: asDouble(customer['creditLimit']),
      invoices: ((json['invoices'] as List?) ?? const [])
          .map((row) => CustomerInvoiceSummary.fromJson(
                Map<String, dynamic>.from(row as Map),
              ))
          .toList(),
    );
  }
}
