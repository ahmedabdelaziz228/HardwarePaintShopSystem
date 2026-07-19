class LookupItem {
  const LookupItem({required this.id, required this.name});

  final String id;
  final String name;

  factory LookupItem.fromJson(Map<String, dynamic> json) => LookupItem(
        id: '${json['id']}',
        name: '${json['name'] ?? ''}',
      );
}

class ProductLookups {
  const ProductLookups({
    this.units = const [],
    this.categories = const [],
    this.priceGroups = const [],
    this.cashboxes = const [],
  });

  final List<LookupItem> units;
  final List<LookupItem> categories;
  final List<LookupItem> priceGroups;
  final List<LookupItem> cashboxes;

  factory ProductLookups.fromJson(Map<String, dynamic> json) => ProductLookups(
        units: _items(json['units']),
        categories: _items(json['categories']),
        priceGroups: _items(json['priceGroups']),
        cashboxes: _items(json['cashboxes']),
      );

  static List<LookupItem> _items(dynamic value) => ((value as List?) ?? const [])
      .map((row) => LookupItem.fromJson(Map<String, dynamic>.from(row as Map)))
      .toList();
}
