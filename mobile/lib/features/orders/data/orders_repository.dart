import 'package:dio/dio.dart';
import 'package:zouq/core/api/api_client.dart';
import 'package:zouq/core/error/exceptions.dart';
import 'package:zouq/features/orders/data/order_dtos.dart';

class OrdersRepository {
  OrdersRepository(this._api);

  final ApiClient _api;

  Future<List<OrderDto>> myOrders() async {
    try {
      final res = await _api.dio.get('/api/orders/mine');
      return _api.unwrap(res, (d) {
        return (d as List)
            .map((e) => OrderDto.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList();
      });
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Network error', statusCode: e.response?.statusCode);
    }
  }

  Future<OrderDto> createOrder({
    required String designId,
    int quantity = 1,
    String? shippingAddressJson,
    String? notes,
  }) async {
    try {
      final res = await _api.dio.post(
        '/api/orders',
        data: {
          'design_id': designId,
          'quantity': quantity,
          if (shippingAddressJson != null) 'shipping_address_json': shippingAddressJson,
          if (notes != null) 'notes': notes,
        },
      );
      return _api.unwrap(
        res,
        (d) => OrderDto.fromJson(Map<String, dynamic>.from(d as Map)),
      );
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Network error', statusCode: e.response?.statusCode);
    }
  }

  Future<OrderDetailDto> getOrder(String orderId) async {
    try {
      final res = await _api.dio.get('/api/orders/$orderId');
      return _api.unwrap(
        res,
        (d) => OrderDetailDto.fromJson(Map<String, dynamic>.from(d as Map)),
      );
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Load order failed', statusCode: e.response?.statusCode);
    }
  }
}
