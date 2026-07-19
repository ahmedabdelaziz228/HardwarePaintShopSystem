import 'dart:convert';

import 'package:hardware_paint_shop_mobile/core/errors/api_exception.dart';
import 'package:path/path.dart';
import 'package:sqflite/sqflite.dart';

/// Cross-feature SQLite cache. It stores JSON only and does not depend on models.
class AppDatabase {
  Database? _database;

  Future<Database> get database async {
    try {
      if (_database != null) return _database!;
      final path = join(
        await getDatabasesPath(),
        'hardware_paint_shop_mobile.db',
      );
      _database = await openDatabase(
        path,
        version: 1,
        onCreate: (db, _) async {
          await db.execute(
            'CREATE TABLE products (id TEXT PRIMARY KEY, json TEXT NOT NULL, updated_at TEXT)',
          );
          await db.execute(
            'CREATE TABLE customers (id TEXT PRIMARY KEY, json TEXT NOT NULL, updated_at TEXT)',
          );
          await db.execute(
            'CREATE TABLE alerts (id TEXT PRIMARY KEY, json TEXT NOT NULL, updated_at TEXT)',
          );
          await db.execute(
            'CREATE TABLE barcodes (id TEXT PRIMARY KEY, product_id TEXT NOT NULL, barcode TEXT NOT NULL, updated_at TEXT)',
          );
          await db.execute('CREATE INDEX ix_barcodes_value ON barcodes(barcode)');
          await db.execute('''CREATE TABLE pending_operations (
            operation_id TEXT PRIMARY KEY,
            type TEXT NOT NULL,
            payload TEXT NOT NULL,
            created_at TEXT NOT NULL,
            status TEXT NOT NULL,
            error TEXT
          )''');
          await db.execute(
            'CREATE TABLE metadata (key TEXT PRIMARY KEY, value TEXT)',
          );
        },
      );
      return _database!;
    } catch (error) {
      throw StorageException('تعذر فتح قاعدة بيانات الهاتف.', cause: error);
    }
  }

  Future<void> saveProducts(Iterable<Map<String, dynamic>> rows) =>
      _guard('تعذر حفظ المنتجات محليًا.', () async {
        final db = await database;
        final batch = db.batch();
        for (final row in rows) {
          batch.insert(
            'products',
            {
              'id': '${row['id']}',
              'json': jsonEncode(row),
              'updated_at':
                  '${row['updatedAt'] ?? DateTime.now().toUtc().toIso8601String()}',
            },
            conflictAlgorithm: ConflictAlgorithm.replace,
          );
        }
        await batch.commit(noResult: true);
      });

  Future<List<Map<String, dynamic>>> searchProductRows(String query) =>
      _guard('تعذر قراءة المنتجات المحفوظة.', () async {
        final db = await database;
        final rows = await db.query(
          'products',
          orderBy: 'updated_at DESC',
          limit: 1000,
        );
        final term = query.trim().toLowerCase();
        final barcodeRows = term.isEmpty
            ? <Map<String, Object?>>[]
            : await db.query(
                'barcodes',
                columns: ['product_id'],
                where: 'barcode = ?',
                whereArgs: [query.trim()],
              );
        final barcodeProductIds = barcodeRows
            .map((row) => '${row['product_id']}')
            .toSet();
        return rows
            .map((row) => jsonDecode('${row['json']}') as Map<String, dynamic>)
            .where((row) {
              if (term.isEmpty) return true;
              final name = '${row['name'] ?? ''}'.toLowerCase();
              final code = '${row['productCode'] ?? ''}'.toLowerCase();
              return name.contains(term) ||
                  code.contains(term) ||
                  barcodeProductIds.contains('${row['id']}');
            })
            .take(100)
            .toList();
      });

  Future<void> saveBarcodes(Iterable<Map<String, dynamic>> rows) =>
      _guard('تعذر حفظ الباركود محليًا.', () async {
        final db = await database;
        final batch = db.batch();
        for (final row in rows) {
          batch.insert(
            'barcodes',
            {
              'id': '${row['id']}',
              'product_id': '${row['productId']}',
              'barcode': '${row['barcode']}',
              'updated_at':
                  '${row['createdAt'] ?? DateTime.now().toUtc().toIso8601String()}',
            },
            conflictAlgorithm: ConflictAlgorithm.replace,
          );
        }
        await batch.commit(noResult: true);
      });

  Future<void> saveCustomers(Iterable<Map<String, dynamic>> rows) =>
      _guard('تعذر حفظ العملاء محليًا.', () async {
        final db = await database;
        final batch = db.batch();
        for (final row in rows) {
          batch.insert(
            'customers',
            {
              'id': '${row['id']}',
              'json': jsonEncode(row),
              'updated_at':
                  '${row['updatedAt'] ?? DateTime.now().toUtc().toIso8601String()}',
            },
            conflictAlgorithm: ConflictAlgorithm.replace,
          );
        }
        await batch.commit(noResult: true);
      });

  Future<List<Map<String, dynamic>>> searchCustomerRows(String query) =>
      _guard('تعذر قراءة العملاء المحفوظين.', () async {
        final db = await database;
        final rows = await db.query(
          'customers',
          orderBy: 'updated_at DESC',
          limit: 2000,
        );
        final term = query.trim().toLowerCase();
        return rows
            .map((row) => jsonDecode('${row['json']}') as Map<String, dynamic>)
            .where((row) {
              if (term.isEmpty) return true;
              return '${row['name'] ?? ''}'.toLowerCase().contains(term) ||
                  '${row['phone'] ?? ''}'.toLowerCase().contains(term);
            })
            .take(150)
            .toList();
      });

  Future<void> saveAlerts(Iterable<Map<String, dynamic>> rows) =>
      _guard('تعذر حفظ التنبيهات محليًا.', () async {
        final db = await database;
        final batch = db.batch();
        for (final row in rows) {
          batch.insert(
            'alerts',
            {
              'id': '${row['id']}',
              'json': jsonEncode(row),
              'updated_at':
                  '${row['createdAt'] ?? DateTime.now().toUtc().toIso8601String()}',
            },
            conflictAlgorithm: ConflictAlgorithm.replace,
          );
        }
        await batch.commit(noResult: true);
      });

  Future<List<Map<String, dynamic>>> alertRows() =>
      _guard('تعذر قراءة التنبيهات المحفوظة.', () async {
        final db = await database;
        final rows = await db.query(
          'alerts',
          orderBy: 'updated_at DESC',
          limit: 500,
        );
        return rows
            .map((row) => jsonDecode('${row['json']}') as Map<String, dynamic>)
            .toList();
      });

  Future<void> queueOperation(Map<String, dynamic> row) =>
      _guard('تعذر حفظ العملية للمزامنة.', () async {
        final db = await database;
        await db.insert(
          'pending_operations',
          row,
          conflictAlgorithm: ConflictAlgorithm.ignore,
        );
      });

  Future<List<Map<String, Object?>>> pendingOperationRows() =>
      _guard('تعذر قراءة العمليات المعلقة.', () async {
        final db = await database;
        return db.query(
          'pending_operations',
          where: 'status != ?',
          whereArgs: ['success'],
          orderBy: 'created_at',
        );
      });

  Future<void> markOperation(
    String id,
    String status, {
    String? error,
  }) =>
      _guard('تعذر تحديث حالة عملية المزامنة.', () async {
        final db = await database;
        await db.update(
          'pending_operations',
          {'status': status, 'error': error},
          where: 'operation_id = ?',
          whereArgs: [id],
        );
      });

  Future<void> removeSuccessfulOperations() =>
      _guard('تعذر تنظيف عمليات المزامنة المكتملة.', () async {
        final db = await database;
        await db.delete(
          'pending_operations',
          where: 'status = ?',
          whereArgs: ['success'],
        );
      });

  Future<DateTime?> lastSync() =>
      _guard('تعذر قراءة تاريخ آخر مزامنة.', () async {
        final db = await database;
        final rows = await db.query(
          'metadata',
          where: 'key = ?',
          whereArgs: ['last_sync'],
          limit: 1,
        );
        if (rows.isEmpty) return null;
        return DateTime.tryParse('${rows.first['value']}');
      });

  Future<void> setLastSync(DateTime value) =>
      _guard('تعذر حفظ تاريخ آخر مزامنة.', () async {
        final db = await database;
        await db.insert(
          'metadata',
          {'key': 'last_sync', 'value': value.toUtc().toIso8601String()},
          conflictAlgorithm: ConflictAlgorithm.replace,
        );
      });

  Future<T> _guard<T>(String message, Future<T> Function() action) async {
    try {
      return await action();
    } catch (error) {
      if (error is StorageException) rethrow;
      throw StorageException(message, cause: error);
    }
  }
}
