import 'dart:convert';

import 'package:path/path.dart';
import 'package:sqflite/sqflite.dart';

import 'models.dart';

class LocalStore {
  Database? _database;

  Future<Database> get database async {
    if (_database != null) return _database!;
    final path = join(await getDatabasesPath(), 'hardware_paint_shop_mobile.db');
    _database = await openDatabase(
      path,
      version: 1,
      onCreate: (db, _) async {
        await db.execute('CREATE TABLE products (id TEXT PRIMARY KEY, json TEXT NOT NULL, updated_at TEXT)');
        await db.execute('CREATE TABLE customers (id TEXT PRIMARY KEY, json TEXT NOT NULL, updated_at TEXT)');
        await db.execute('CREATE TABLE alerts (id TEXT PRIMARY KEY, json TEXT NOT NULL, updated_at TEXT)');
        await db.execute('CREATE TABLE barcodes (id TEXT PRIMARY KEY, product_id TEXT NOT NULL, barcode TEXT NOT NULL, updated_at TEXT)');
        await db.execute('CREATE INDEX ix_barcodes_value ON barcodes(barcode)');
        await db.execute('''CREATE TABLE pending_operations (
          operation_id TEXT PRIMARY KEY,
          type TEXT NOT NULL,
          payload TEXT NOT NULL,
          created_at TEXT NOT NULL,
          status TEXT NOT NULL,
          error TEXT
        )''');
        await db.execute('CREATE TABLE metadata (key TEXT PRIMARY KEY, value TEXT)');
      },
    );
    return _database!;
  }

  Future<void> saveProducts(Iterable<Map<String, dynamic>> rows) async {
    final db = await database;
    final batch = db.batch();
    for (final row in rows) {
      batch.insert('products', {
        'id': '${row['id']}',
        'json': jsonEncode(row),
        'updated_at': '${row['updatedAt'] ?? DateTime.now().toUtc().toIso8601String()}',
      }, conflictAlgorithm: ConflictAlgorithm.replace);
    }
    await batch.commit(noResult: true);
  }

  Future<List<ProductSummary>> searchProducts(String query) async {
    final db = await database;
    final rows = await db.query('products', orderBy: 'updated_at DESC', limit: 1000);
    final term = query.trim().toLowerCase();
    final barcodeRows = term.isEmpty
        ? <Map<String, Object?>>[]
        : await db.query('barcodes', columns: ['product_id'], where: 'barcode = ?', whereArgs: [query.trim()]);
    final barcodeProductIds = barcodeRows.map((r) => '${r['product_id']}').toSet();
    return rows
        .map((r) => ProductSummary.fromJson(jsonDecode('${r['json']}') as Map<String, dynamic>))
        .where((p) => term.isEmpty || p.name.toLowerCase().contains(term) ||
            (p.code?.toLowerCase().contains(term) ?? false) || barcodeProductIds.contains(p.id))
        .take(100)
        .toList();
  }

  Future<void> saveBarcodes(Iterable<Map<String, dynamic>> rows) async {
    final db = await database;
    final batch = db.batch();
    for (final row in rows) {
      batch.insert('barcodes', {
        'id': '${row['id']}',
        'product_id': '${row['productId']}',
        'barcode': '${row['barcode']}',
        'updated_at': '${row['createdAt'] ?? DateTime.now().toUtc().toIso8601String()}',
      }, conflictAlgorithm: ConflictAlgorithm.replace);
    }
    await batch.commit(noResult: true);
  }

  Future<void> saveCustomers(Iterable<Map<String, dynamic>> rows) async {
    final db = await database;
    final batch = db.batch();
    for (final row in rows) {
      batch.insert('customers', {
        'id': '${row['id']}',
        'json': jsonEncode(row),
        'updated_at': '${row['updatedAt'] ?? DateTime.now().toUtc().toIso8601String()}',
      }, conflictAlgorithm: ConflictAlgorithm.replace);
    }
    await batch.commit(noResult: true);
  }

  Future<List<CustomerSummary>> searchCustomers(String query) async {
    final db = await database;
    final rows = await db.query('customers', orderBy: 'updated_at DESC', limit: 2000);
    final term = query.trim().toLowerCase();
    return rows
        .map((r) => CustomerSummary.fromJson(jsonDecode('${r['json']}') as Map<String, dynamic>))
        .where((c) => term.isEmpty || c.name.toLowerCase().contains(term) ||
            (c.phone?.toLowerCase().contains(term) ?? false))
        .take(150)
        .toList();
  }

  Future<void> saveAlerts(Iterable<Map<String, dynamic>> rows) async {
    final db = await database;
    final batch = db.batch();
    for (final row in rows) {
      batch.insert('alerts', {
        'id': '${row['id']}',
        'json': jsonEncode(row),
        'updated_at': '${row['createdAt'] ?? DateTime.now().toUtc().toIso8601String()}',
      }, conflictAlgorithm: ConflictAlgorithm.replace);
    }
    await batch.commit(noResult: true);
  }

  Future<List<ShopAlert>> getAlerts() async {
    final db = await database;
    final rows = await db.query('alerts', orderBy: 'updated_at DESC', limit: 500);
    return rows.map((r) => ShopAlert.fromJson(jsonDecode('${r['json']}') as Map<String, dynamic>)).toList();
  }

  Future<void> queue(PendingOperation operation) async {
    final db = await database;
    await db.insert('pending_operations', operation.toDbJson(), conflictAlgorithm: ConflictAlgorithm.ignore);
  }

  Future<List<PendingOperation>> pendingOperations() async {
    final db = await database;
    final rows = await db.query('pending_operations', where: 'status != ?', whereArgs: ['success'], orderBy: 'created_at');
    return rows.map(PendingOperation.fromDb).toList();
  }

  Future<void> markOperation(String id, String status, {String? error}) async {
    final db = await database;
    await db.update('pending_operations', {'status': status, 'error': error}, where: 'operation_id = ?', whereArgs: [id]);
  }

  Future<void> removeSuccessfulOperations() async {
    final db = await database;
    await db.delete('pending_operations', where: 'status = ?', whereArgs: ['success']);
  }

  Future<DateTime?> lastSync() async {
    final db = await database;
    final rows = await db.query('metadata', where: 'key = ?', whereArgs: ['last_sync'], limit: 1);
    if (rows.isEmpty) return null;
    return DateTime.tryParse('${rows.first['value']}');
  }

  Future<void> setLastSync(DateTime value) async {
    final db = await database;
    await db.insert('metadata', {'key': 'last_sync', 'value': value.toUtc().toIso8601String()},
        conflictAlgorithm: ConflictAlgorithm.replace);
  }
}
