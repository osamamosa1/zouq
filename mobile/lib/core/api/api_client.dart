import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ApiClient {
  ApiClient(this._dio, this._storage) {
    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await _storage.read(key: 'access_token');
        if (token != null && token.isNotEmpty) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        handler.next(options);
      },
    ));
  }

  final Dio _dio;
  final FlutterSecureStorage _storage;

  Dio get dio => _dio;

  Future<T> unwrap<T>(Response response, T Function(dynamic data) map) {
    final body = response.data;
    if (body is Map && body['status'] == 'error') {
      throw DioException(
        requestOptions: response.requestOptions,
        message: body['message']?.toString() ?? 'Request failed',
      );
    }
    final data = body is Map ? body['data'] : body;
    return Future.value(map(data));
  }
}
