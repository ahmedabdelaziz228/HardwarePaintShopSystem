/// Base exception exposed by the data layer to Cubits.
class AppException implements Exception {
  const AppException(this.message, {this.cause});

  final String message;
  final Object? cause;

  @override
  String toString() => message;
}

/// A valid API response that represents a business or authorization failure.
class ApiException extends AppException {
  const ApiException(super.message, {this.statusCode, super.cause});

  final int? statusCode;
}

/// A timeout, DNS, socket, or unreachable-host failure.
class NetworkException extends AppException {
  const NetworkException(super.message, {super.cause});
}

/// A local SQLite, secure-storage, or preferences failure.
class StorageException extends AppException {
  const StorageException(super.message, {super.cause});
}
