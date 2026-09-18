import 'package:equatable/equatable.dart';
import 'package:zouq/features/catalog/data/catalog_dtos.dart';
import 'package:zouq/features/design_editor/domain/normalized_coordinates.dart';
import 'package:zouq/features/design_editor/domain/production_method.dart';

class EditorElement extends Equatable {
  const EditorElement({
    required this.id,
    required this.surfaceCode,
    this.designAssetId,
    this.userUploadId,
    required this.productionMethod,
    required this.normX,
    required this.normY,
    required this.normWidth,
    required this.normHeight,
    this.rotationDegrees = 0,
    this.scale = 1,
    required this.zIndex,
    this.label,
  });

  final String id;
  final String surfaceCode;
  final String? designAssetId;
  final String? userUploadId;
  final ProductionMethod productionMethod;
  final double normX;
  final double normY;
  final double normWidth;
  final double normHeight;
  final double rotationDegrees;
  final double scale;
  final int zIndex;
  final String? label;

  factory EditorElement.placeholder({
    required String surfaceCode,
    required int zIndex,
  }) {
    return EditorElement(
      id: '${surfaceCode}_${DateTime.now().microsecondsSinceEpoch}',
      surfaceCode: surfaceCode,
      productionMethod: ProductionMethod.printing,
      normX: 0.25,
      normY: 0.25,
      normWidth: 0.5,
      normHeight: 0.5,
      zIndex: zIndex,
      label: 'Design',
    );
  }

  EditorElement copyWith({
    String? surfaceCode,
    String? designAssetId,
    String? userUploadId,
    ProductionMethod? productionMethod,
    double? normX,
    double? normY,
    double? normWidth,
    double? normHeight,
    double? rotationDegrees,
    int? zIndex,
    String? label,
  }) {
    return EditorElement(
      id: id,
      surfaceCode: surfaceCode ?? this.surfaceCode,
      designAssetId: designAssetId ?? this.designAssetId,
      userUploadId: userUploadId ?? this.userUploadId,
      productionMethod: productionMethod ?? this.productionMethod,
      normX: normX ?? this.normX,
      normY: normY ?? this.normY,
      normWidth: normWidth ?? this.normWidth,
      normHeight: normHeight ?? this.normHeight,
      rotationDegrees: rotationDegrees ?? this.rotationDegrees,
      scale: scale,
      zIndex: zIndex ?? this.zIndex,
      label: label ?? this.label,
    );
  }

  Map<String, dynamic> toSaveJson(DesignAreaDto designArea) {
    final real = NormalizedCoordinates.realDimensions(
      designArea: designArea,
      normWidth: normWidth,
      normHeight: normHeight,
    );
    return {
      'surface_code': surfaceCode,
      if (designAssetId != null) 'design_asset_id': designAssetId,
      if (userUploadId != null) 'user_upload_id': userUploadId,
      'production_method': productionMethod.apiValue,
      'norm_x': normX,
      'norm_y': normY,
      'norm_width': normWidth,
      'norm_height': normHeight,
      'rotation_degrees': rotationDegrees,
      'scale': scale,
      'real_width': real.realWidth,
      'real_height': real.realHeight,
      'z_index': zIndex,
    };
  }

  Map<String, dynamic> toQuoteJson(DesignAreaDto designArea) {
    return {
      'surface_code': surfaceCode,
      'production_method': productionMethod.apiValue,
      'norm_x': normX,
      'norm_y': normY,
      'norm_width': normWidth,
      'norm_height': normHeight,
    };
  }

  @override
  List<Object?> get props => [id, surfaceCode, normX, normY, normWidth, normHeight, productionMethod];
}
