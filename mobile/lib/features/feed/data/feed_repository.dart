import 'package:dio/dio.dart';
import 'package:zouq/core/api/api_client.dart';
import 'package:zouq/core/error/exceptions.dart';
import 'package:zouq/features/feed/data/feed_dtos.dart';

class FeedRepository {
  FeedRepository(this._api);

  final ApiClient _api;

  Future<List<FeedItemDto>> forYou({int take = 40, int skip = 0}) async {
    try {
      final res = await _api.dio.get(
        '/api/feed/for-you',
        queryParameters: {'take': take, 'skip': skip},
      );
      return _api.unwrap(res, (d) {
        return (d as List)
            .map((e) => FeedItemDto.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList();
      });
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Network error', statusCode: e.response?.statusCode);
    }
  }

  Future<List<AdDto>> ads() async {
    try {
      final res = await _api.dio.get('/api/feed/ads');
      return _api.unwrap(res, (d) {
        return (d as List)
            .map((e) => AdDto.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList();
      });
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Network error', statusCode: e.response?.statusCode);
    }
  }
}
