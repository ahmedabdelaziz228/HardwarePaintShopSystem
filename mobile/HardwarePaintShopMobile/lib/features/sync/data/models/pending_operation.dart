import 'dart:convert';

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

  factory PendingOperation.fromDb(Map<String, Object?> row) => PendingOperation(
        operationId: '${row['operation_id']}',
        type: '${row['type']}',
        payload: jsonDecode('${row['payload']}') as Map<String, dynamic>,
        createdAt: DateTime.parse('${row['created_at']}'),
        status: '${row['status']}',
        error: row['error']?.toString(),
      );
}
