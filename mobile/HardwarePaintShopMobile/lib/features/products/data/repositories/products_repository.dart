import 'package:hardware_paint_shop_mobile/core/errors/api_exception.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/network/api_client.dart';
import 'package:hardware_paint_shop_mobile/core/storage/app_database.dart';
import 'package:hardware_paint_shop_mobile/core/storage/offline_operation_queue.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/models/product_details.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/models/product_lookups.dart';
import 'package:hardware_paint_shop_mobile/features/products/data/models/product_summary.dart';

class ProductSearchResult {
  const ProductSearchResult({
    required this.products,
    required this.online,
    this.warning,
  });

  final List<ProductSummary> products;
  final bool online;
  final String? warning;
}

class ProductWriteResult {
  const ProductWriteResult({required this.online});

  final bool online;
}

/// Product remote/local data boundary with offline write queuing.
class ProductsRepository {
  ProductsRepository(
    this._apiClient,
    this._database,
    this._operationQueue,
  );

  final ApiClient _apiClient;
  final AppDatabase _database;
  final OfflineOperationQueue _operationQueue;
  ProductLookups? _lookups;

  String imageUrl(String productId) => _apiClient.imageUrl(productId);
  Map<String, String> get imageHeaders => _apiClient.imageHeaders;

  Future<ProductSearchResult> search(String query) async {
    try {
      final rows = await _apiClient.get<List<dynamic>>(
        '/api/products/search',
        query: {'query': query, 'limit': 100},
      );
      final jsonRows = rows
          .map((row) => Map<String, dynamic>.from(row as Map))
          .toList();
      await _database.saveProducts(jsonRows);
      return ProductSearchResult(
        products: jsonRows.map(ProductSummary.fromJson).toList(),
        online: true,
      );
    } on NetworkException catch (error) {
      final cachedRows = await _database.searchProductRows(query);
      if (cachedRows.isEmpty) rethrow;
      return ProductSearchResult(
        products: cachedRows.map(ProductSummary.fromJson).toList(),
        online: false,
        warning: '${error.message} عرض نتائج محفوظة.',
      );
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<ProductDetails> details(String productId) async {
    try {
      final json = await _apiClient.get<Map<String, dynamic>>(
        '/api/products/$productId',
      );
      return ProductDetails.fromJson(json);
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<ProductSummary> scan(String value, {bool serial = false}) async {
    try {
      final path = serial
          ? '/api/products/serial/${Uri.encodeComponent(value)}'
          : '/api/products/barcode/${Uri.encodeComponent(value)}';
      final json = await _apiClient.get<Map<String, dynamic>>(path);
      return ProductSummary.fromJson(json);
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<ProductLookups> loadLookups({bool force = false}) async {
    try {
      if (!force && _lookups != null) return _lookups!;
      final json = await _apiClient.get<Map<String, dynamic>>('/api/lookups');
      _lookups = ProductLookups.fromJson(json);
      return _lookups!;
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<ProductWriteResult> create(Map<String, dynamic> payload) async {
    try {
      final online = await _operationQueue.sendOrQueue(
        type: 'product_create',
        payload: payload,
        onlineAction: () async {
          await _apiClient.post<dynamic>('/api/products', body: payload);
        },
      );
      return ProductWriteResult(online: online);
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<ProductWriteResult> adjustStock(
    String productId,
    double difference,
    String reason,
  ) async {
    final payload = {
      'productId': productId,
      'quantityBase': difference,
      'reason': reason,
      'operationDate': DateTime.now().toUtc().toIso8601String(),
    };
    try {
      final online = await _operationQueue.sendOrQueue(
        type: 'stock_adjustment',
        payload: payload,
        onlineAction: () async {
          await _apiClient.post<dynamic>(
            '/api/stock/adjustment',
            body: payload,
          );
        },
      );
      return ProductWriteResult(online: online);
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }
}
