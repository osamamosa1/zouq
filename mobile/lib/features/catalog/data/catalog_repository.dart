import 'package:dio/dio.dart';
import 'package:zouq/core/api/api_client.dart';
import 'package:zouq/core/error/exceptions.dart';
import 'package:zouq/features/catalog/data/catalog_dtos.dart';

class CatalogRepository {
  CatalogRepository(this._api);

  final ApiClient _api;

  Future<List<ProductListItemDto>> listProducts() async {
    try {
      final res = await _api.dio.get('/api/products');
      return _api.unwrap(res, (d) {
        return (d as List)
            .map((e) => ProductListItemDto.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList();
      });
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Network error', statusCode: e.response?.statusCode);
    }
  }

  Future<ProductConfigDto> getProductConfig(String productId) async {
    try {
      final res = await _api.dio.get('/api/products/$productId/config');
      return _api.unwrap(
        res,
        (d) => ProductConfigDto.fromJson(Map<String, dynamic>.from(d as Map)),
      );
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Network error', statusCode: e.response?.statusCode);
    }
  }
}
