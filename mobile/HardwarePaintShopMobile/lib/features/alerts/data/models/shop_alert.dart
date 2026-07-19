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
        createdAt: DateTime.tryParse('${json['createdAt'] ?? ''}') ??
            DateTime.now(),
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
