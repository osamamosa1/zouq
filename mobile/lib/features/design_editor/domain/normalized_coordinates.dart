import 'package:zouq/features/catalog/data/catalog_dtos.dart';

/// Converts element norms (0..1 within the design area) to real production dimensions.
class NormalizedCoordinates {
  NormalizedCoordinates._();

  static ({double realWidth, double realHeight}) realDimensions({
    required DesignAreaDto designArea,
    required double normWidth,
    required double normHeight,
  }) {
    return (
      realWidth: normWidth * designArea.realWidth,
      realHeight: normHeight * designArea.realHeight,
    );
  }

  /// Maps element normalized position within design area to normalized position on surface preview.
  static RectNorm mapElementToSurface({
    required DesignAreaDto designArea,
    required double elementNormX,
    required double elementNormY,
    required double elementNormWidth,
    required double elementNormHeight,
  }) {
    return RectNorm(
      x: designArea.normX + elementNormX * designArea.normWidth,
      y: designArea.normY + elementNormY * designArea.normHeight,
      width: elementNormWidth * designArea.normWidth,
      height: elementNormHeight * designArea.normHeight,
    );
  }

  /// Inverse: pointer position on design area (0..1) from surface-normalized rect.
  static RectNorm surfaceToElement({
    required DesignAreaDto designArea,
    required RectNorm onSurface,
  }) {
    final dx = designArea.normWidth == 0 ? 0.0 : (onSurface.x - designArea.normX) / designArea.normWidth;
    final dy = designArea.normHeight == 0 ? 0.0 : (onSurface.y - designArea.normY) / designArea.normHeight;
    final dw = designArea.normWidth == 0 ? 0.0 : onSurface.width / designArea.normWidth;
    final dh = designArea.normHeight == 0 ? 0.0 : onSurface.height / designArea.normHeight;
    return RectNorm(
      x: dx.clamp(0.0, 1.0),
      y: dy.clamp(0.0, 1.0),
      width: dw.clamp(0.05, 1.0),
      height: dh.clamp(0.05, 1.0),
    );
  }
}

class RectNorm {
  const RectNorm({
    required this.x,
    required this.y,
    required this.width,
    required this.height,
  });

  final double x;
  final double y;
  final double width;
  final double height;

  RectNorm copyWith({double? x, double? y, double? width, double? height}) {
    return RectNorm(
      x: x ?? this.x,
      y: y ?? this.y,
      width: width ?? this.width,
      height: height ?? this.height,
    );
  }
}
