class PriceBreakdownDto {
  PriceBreakdownDto({
    required this.baseProductPrice,
    required this.fabricAdjustment,
    required this.cutAdjustment,
    required this.sizeAdjustment,
    required this.printingCost,
    required this.embroideryCost,
    required this.total,
    required this.lines,
  });

  final double baseProductPrice;
  final double fabricAdjustment;
  final double cutAdjustment;
  final double sizeAdjustment;
  final double printingCost;
  final double embroideryCost;
  final double total;
  final List<PriceLineDto> lines;

  factory PriceBreakdownDto.fromJson(Map<String, dynamic> json) {
    return PriceBreakdownDto(
      baseProductPrice: (json['base_product_price'] as num).toDouble(),
      fabricAdjustment: (json['fabric_adjustment'] as num).toDouble(),
      cutAdjustment: (json['cut_adjustment'] as num).toDouble(),
      sizeAdjustment: (json['size_adjustment'] as num).toDouble(),
      printingCost: (json['printing_cost'] as num).toDouble(),
      embroideryCost: (json['embroidery_cost'] as num).toDouble(),
      total: (json['total'] as num).toDouble(),
      lines: (json['lines'] as List<dynamic>)
          .map((e) => PriceLineDto.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}

class PriceLineDto {
  PriceLineDto({required this.code, required this.label, required this.amount});

  final String code;
  final String label;
  final double amount;

  factory PriceLineDto.fromJson(Map<String, dynamic> json) {
    return PriceLineDto(
      code: json['code'] as String,
      label: json['label'] as String,
      amount: (json['amount'] as num).toDouble(),
    );
  }
}

class DesignDto {
  DesignDto({
    required this.id,
    required this.title,
    required this.productId,
    required this.status,
    required this.visibility,
    this.description,
    this.previewImageUrl,
    this.lastEstimatedPrice,
    this.derivedFromDesignId,
    this.fabricId,
    this.cutStyleId,
    this.productSizeId,
    this.printingOptionId,
    this.isFeatured = false,
    this.createdAtUtc,
    this.updatedAtUtc,
    this.elements = const [],
  });

  final String id;
  final String title;
  final String productId;
  final String status;
  final String visibility;
  final String? description;
  final String? previewImageUrl;
  final double? lastEstimatedPrice;
  final String? derivedFromDesignId;
  final String? fabricId;
  final String? cutStyleId;
  final String? productSizeId;
  final String? printingOptionId;
  final bool isFeatured;
  final DateTime? createdAtUtc;
  final DateTime? updatedAtUtc;
  final List<DesignElementDto> elements;

  bool get isDraft => status.toLowerCase() == 'draft' || status.toLowerCase() == 'unpublished';
  bool get isReadyToPublish => status.toLowerCase() == 'deliveredeligible';
  bool get isPublished => status.toLowerCase() == 'reusable';
  bool get isDerived => derivedFromDesignId != null;

  factory DesignDto.fromJson(Map<String, dynamic> json) {
    return DesignDto(
      id: json['id'] as String,
      title: json['title'] as String,
      productId: json['product_id'] as String,
      status: json['status'] as String? ?? 'Draft',
      visibility: json['visibility'] as String? ?? 'Private',
      description: json['description'] as String?,
      previewImageUrl: json['preview_image_url'] as String?,
      lastEstimatedPrice: json['last_estimated_price'] == null
          ? null
          : (json['last_estimated_price'] as num).toDouble(),
      derivedFromDesignId: json['derived_from_design_id'] as String?,
      fabricId: json['fabric_id'] as String?,
      cutStyleId: json['cut_style_id'] as String?,
      productSizeId: json['product_size_id'] as String?,
      printingOptionId: json['printing_option_id'] as String?,
      isFeatured: json['is_featured'] as bool? ?? false,
      createdAtUtc: json['created_at_utc'] == null
          ? null
          : DateTime.tryParse(json['created_at_utc'] as String),
      updatedAtUtc: json['updated_at_utc'] == null
          ? null
          : DateTime.tryParse(json['updated_at_utc'] as String),
      elements: (json['elements'] as List<dynamic>? ?? [])
          .map((e) => DesignElementDto.fromJson(Map<String, dynamic>.from(e as Map)))
          .toList(),
    );
  }
}

class DesignElementDto {
  DesignElementDto({
    required this.surfaceCode,
    required this.productionMethod,
    required this.normX,
    required this.normY,
    required this.normWidth,
    required this.normHeight,
    this.designAssetId,
    this.userUploadId,
    this.rotationDegrees = 0,
    this.scale = 1,
    this.zIndex = 0,
    this.realWidth,
    this.realHeight,
  });

  final String surfaceCode;
  final String productionMethod;
  final double normX;
  final double normY;
  final double normWidth;
  final double normHeight;
  final String? designAssetId;
  final String? userUploadId;
  final double rotationDegrees;
  final double scale;
  final int zIndex;
  final double? realWidth;
  final double? realHeight;

  factory DesignElementDto.fromJson(Map<String, dynamic> json) {
    return DesignElementDto(
      surfaceCode: json['surface_code'] as String,
      productionMethod: json['production_method'] as String? ?? 'Printing',
      normX: (json['norm_x'] as num).toDouble(),
      normY: (json['norm_y'] as num).toDouble(),
      normWidth: (json['norm_width'] as num).toDouble(),
      normHeight: (json['norm_height'] as num).toDouble(),
      designAssetId: json['design_asset_id'] as String?,
      userUploadId: json['user_upload_id'] as String?,
      rotationDegrees: (json['rotation_degrees'] as num?)?.toDouble() ?? 0,
      scale: (json['scale'] as num?)?.toDouble() ?? 1,
      zIndex: (json['z_index'] as num?)?.toInt() ?? 0,
      realWidth: (json['real_width'] as num?)?.toDouble(),
      realHeight: (json['real_height'] as num?)?.toDouble(),
    );
  }
}

class DesignAssetDto {
  DesignAssetDto({
    required this.id,
    required this.name,
    required this.fileUrl,
    this.thumbnailUrl,
    this.categoryName,
    this.tags = const [],
  });

  final String id;
  final String name;
  final String fileUrl;
  final String? thumbnailUrl;
  final String? categoryName;
  final List<String> tags;

  factory DesignAssetDto.fromJson(Map<String, dynamic> json) {
    return DesignAssetDto(
      id: json['id'] as String,
      name: json['name'] as String,
      fileUrl: json['file_url'] as String,
      thumbnailUrl: json['thumbnail_url'] as String?,
      categoryName: json['category_name'] as String?,
      tags: (json['tags'] as List<dynamic>? ?? []).map((e) => e.toString()).toList(),
    );
  }
}
