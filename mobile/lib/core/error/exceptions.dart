class ServerException implements Exception {
  ServerException(this.message, {this.statusCode});
  final String message;
  final int? statusCode;

  @override
  String toString() => message;
}

class CacheException implements Exception {
  CacheException(this.message);
  final String message;
}
