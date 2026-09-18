import 'package:dio/dio.dart';
import 'package:zouq/core/api/api_client.dart';
import 'package:zouq/core/error/exceptions.dart';
import 'package:zouq/features/design_editor/data/design_dtos.dart';

class DesignRepository {
  DesignRepository(this._api);

  final ApiClient _api;

  Future<PriceBreakdownDto> quote(Map<String, dynamic> body) async {
    try {
      final res = await _api.dio.post('/api/designs/quote', data: body);
      return _api.unwrap(
        res,
        (d) => PriceBreakdownDto.fromJson(Map<String, dynamic>.from(d as Map)),
      );
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Quote failed', statusCode: e.response?.statusCode);
    }
  }

  Future<DesignDto> saveDesign(Map<String, dynamic> body, {String? designId}) async {
    try {
      final res = designId == null
          ? await _api.dio.post('/api/designs', data: body)
          : await _api.dio.put('/api/designs/$designId', data: body);
      return _api.unwrap(
        res,
        (d) => DesignDto.fromJson(Map<String, dynamic>.from(d as Map)),
      );
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Save failed', statusCode: e.response?.statusCode);
    }
  }

  Future<({String id, String fileUrl})> uploadImage(String filePath, String fileName) async {
    try {
      final form = FormData.fromMap({
        'file': await MultipartFile.fromFile(filePath, filename: fileName),
      });
      final res = await _api.dio.post(
        '/api/uploads',
        data: form,
        options: Options(contentType: 'multipart/form-data'),
      );
      return _api.unwrap(res, (d) {
        final map = Map<String, dynamic>.from(d as Map);
        return (id: map['id'] as String, fileUrl: map['file_url'] as String);
      });
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Upload failed', statusCode: e.response?.statusCode);
    }
  }

  Future<void> publish(String designId) async {
    try {
      await _api.dio.post('/api/designs/$designId/publish');
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Publish failed', statusCode: e.response?.statusCode);
    }
  }

  Future<DesignDto> derive(String sourceDesignId) async {
    try {
      final res = await _api.dio.post('/api/designs/$sourceDesignId/derive');
      return _api.unwrap(
        res,
        (d) => DesignDto.fromJson(Map<String, dynamic>.from(d as Map)),
      );
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Use design failed', statusCode: e.response?.statusCode);
    }
  }

  Future<DesignDto> getDesign(String designId) async {
    try {
      final res = await _api.dio.get('/api/designs/$designId');
      return _api.unwrap(
        res,
        (d) => DesignDto.fromJson(Map<String, dynamic>.from(d as Map)),
      );
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Load design failed', statusCode: e.response?.statusCode);
    }
  }

  Future<List<DesignDto>> myDesigns() async {
    try {
      final res = await _api.dio.get('/api/designs/mine');
      return _api.unwrap(res, (d) {
        return (d as List)
            .map((e) => DesignDto.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList();
      });
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Load designs failed', statusCode: e.response?.statusCode);
    }
  }

  Future<void> unpublish(String designId) async {
    try {
      await _api.dio.post('/api/designs/$designId/unpublish');
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Unpublish failed', statusCode: e.response?.statusCode);
    }
  }

  Future<List<DesignAssetDto>> listAssets({String? productId, String? surface}) async {
    try {
      final res = await _api.dio.get(
        '/api/assets',
        queryParameters: {
          if (productId != null) 'productId': productId,
          if (surface != null) 'surface': surface,
        },
      );
      return _api.unwrap(res, (d) {
        return (d as List)
            .map((e) => DesignAssetDto.fromJson(Map<String, dynamic>.from(e as Map)))
            .toList();
      });
    } on DioException catch (e) {
      throw ServerException(e.message ?? 'Load assets failed', statusCode: e.response?.statusCode);
    }
  }
}
