import 'dart:convert';

import 'package:hardware_paint_shop_mobile/core/errors/api_exception.dart';
import 'package:hardware_paint_shop_mobile/core/errors/error_mapper.dart';
import 'package:hardware_paint_shop_mobile/core/storage/app_database.dart';
import 'package:hardware_paint_shop_mobile/core/utils/id_generator.dart';

/// Executes a write online or stores it atomically for the sync feature.
class OfflineOperationQueue {
  OfflineOperationQueue(this._database);

  final AppDatabase _database;

  Future<bool> sendOrQueue({
    required String type,
    required Map<String, dynamic> payload,
    required Future<void> Function() onlineAction,
  }) async {
    try {
      await onlineAction();
      return true;
    } on ApiException catch (error) {
      if (error.statusCode != null) rethrow;
      await _queue(type, payload);
      return false;
    } on NetworkException {
      await _queue(type, payload);
      return false;
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }

  Future<void> _queue(String type, Map<String, dynamic> payload) async {
    try {
      await _database.queueOperation({
        'operation_id': IdGenerator.uuidV4(),
        'type': type,
        'payload': jsonEncode(payload),
        'created_at': DateTime.now().toUtc().toIso8601String(),
        'status': 'pending',
        'error': null,
      });
    } catch (error) {
      throw ErrorMapper.map(error);
    }
  }
}
