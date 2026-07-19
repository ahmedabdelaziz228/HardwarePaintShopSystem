class CashboxOption {
  const CashboxOption({required this.id, required this.name});

  final String id;
  final String name;

  factory CashboxOption.fromJson(Map<String, dynamic> json) => CashboxOption(
        id: '${json['id']}',
        name: '${json['name'] ?? ''}',
      );
}
