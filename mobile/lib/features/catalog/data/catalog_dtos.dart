class ProductListItemDto {
  ProductListItemDto({
    required this.id,
    required this.name,
    required this.slug,
    this.thumbnailUrl,
    required this.basePrice,
    required this.productTypeName,
    required this.measurementUnit,
  });

  final String id;
  final String name;
  final String slug;
  final String? thumbnailUrl;
  final double basePrice;
  final String productTypeName;
  final String measurementUnit;

  factory ProductListItemDto.fromJson(Map<String, dynamic> json) {
    return ProductListItemDto(
      id: json['id'] as String,
      name: json['name'] as String,
      slug: json['slug'] as String,
      thumbnailUrl: json['thumbnail_url'] as String?,
      basePrice: (json['base_price'] as num).toDouble(),
      productTypeName: json['product_type_name'] as String,
      measurementUnit: json['measurement_unit'] as String,
    );
  }
}

class DesignAreaDto {
  DesignAreaDto({
    required this.normX,
    required this.normY,
    required this.normWidth,
    required this.normHeight,
    required this.realWidth,
    required this.realHeight,
  });

  final double normX;
  final double normY;
  final double normWidth;
  final double normHeight;
  final double realWidth;
  final double realHeight;

  factory DesignAreaDto.fromJson(Map<String, dynamic> json) {
    return DesignAreaDto(
      normX: (json['norm_x'] as num).toDouble(),
      normY: (json['norm_y'] as num).toDouble(),
      normWidth: (json['norm_width'] as num).toDouble(),
      normHeight: (json['norm_height'] as num).toDouble(),
      realWidth: (json['real_width'] as num).toDouble(),
      realHeight: (json['real_height'] as num).toDouble(),
    );
  }
}

class SurfaceDto {
  SurfaceDto({
    required this.id,
    required this.code,
    required this.name,
    this.previewImageUrl,
    required this.isRequired,
    this.designArea,
  });

  final String id;
  final String code;
  final String name;
  final String? previewImageUrl;
  final bool isRequired;
  final DesignAreaDto? designArea;

  factory SurfaceDto.fromJson(Map<String, dynamic> json) {
    return SurfaceDto(
      id: json['id'] as String,
      code: json['code'] as String,
      name: json['name'] as String,
      previewImageUrl: json['preview_image_url'] as String?,
      isRequired: json['is_required'] as bool? ?? false,
      designArea: json['design_area'] == null
          ? null
          : DesignAreaDto.fromJson(json['design_area'] as Map<String, dynamic>),
    );
  }
}

class OptionAdjustmentDto {
  OptionAdjustmentDto({
    required this.id,
    required this.name,
    this.description,
    this.imageUrl,
    required this.priceAdjustment,
  });

  final String id;
  final String name;
  final String? description;
  final String? imageUrl;
  final double priceAdjustment;

  factory OptionAdjustmentDto.fromJson(Map<String, dynamic> json) {
    return OptionAdjustmentDto(
      id: json['id'] as String,
      name: json['name'] as String,
      description: json['description'] as String?,
      imageUrl: json['image_url'] as String?,
      priceAdjustment: (json['price_adjustment'] as num).toDouble(),
    );
  }
}

class SizeDto {
  SizeDto({
    required this.id,
    required this.code,
    required this.name,
    required this.width,
    required this.height,
    this.depth,
    required this.unit,
    required this.priceAdjustment,
  });

  final String id;
  final String code;
  final String name;
  final double width;
  final double height;
  final double? depth;
  final String unit;
  final double priceAdjustment;

  factory SizeDto.fromJson(Map<String, dynamic> json) {
    return SizeDto(
      id: json['id'] as String,
      code: json['code'] as String,
      name: json['name'] as String,
      width: (json['width'] as num).toDouble(),
      height: (json['height'] as num).toDouble(),
      depth: json['depth'] == null ? null : (json['depth'] as num).toDouble(),
      unit: json['unit'] as String,
      priceAdjustment: (json['price_adjustment'] as num).toDouble(),
    );
  }
}

class PrintingOptionDto {
  PrintingOptionDto({
    required this.id,
    required this.name,
    required this.code,
    this.description,
    required this.price,
    required this.includedSurfaceCodes,
  });

  final String id;
  final String name;
  final String code;
  final String? description;
  final double price;
  final List<String> includedSurfaceCodes;

  factory PrintingOptionDto.fromJson(Map<String, dynamic> json) {
    return PrintingOptionDto(
      id: json['id'] as String,
      name: json['name'] as String,
      code: json['code'] as String,
      description: json['description'] as String?,
      price: (json['price'] as num).toDouble(),
      includedSurfaceCodes: (json['included_surface_codes'] as List<dynamic>)
          .map((e) => e as String)
          .toList(),
    );
  }
}

class EmbroideryPricingDto {
  EmbroideryPricingDto({
    required this.pricePerSquareUnit,
    required this.unit,
    this.minimumCharge,
  });

  final double pricePerSquareUnit;
  final String unit;
  final double? minimumCharge;

  factory EmbroideryPricingDto.fromJson(Map<String, dynamic> json) {
    return EmbroideryPricingDto(
      pricePerSquareUnit: (json['price_per_square_unit'] as num).toDouble(),
      unit: json['unit'] as String,
      minimumCharge:
          json['minimum_charge'] == null ? null : (json['minimum_charge'] as num).toDouble(),
    );
  }
}

class ProductConfigDto {
  ProductConfigDto({
    required this.id,
    required this.name,
    required this.slug,
    this.description,
    this.thumbnailUrl,
    required this.basePrice,
    required this.measurementUnit,
    required this.productTypeName,
    required this.surfaces,
    required this.fabrics,
    required this.cuts,
    required this.sizes,
    required this.printingOptions,
    this.embroideryPricing,
  });

  final String id;
  final String name;
  final String slug;
  final String? description;
  final String? thumbnailUrl;
  final double basePrice;
  final String measurementUnit;
  final String productTypeName;
  final List<SurfaceDto> surfaces;
  final List<OptionAdjustmentDto> fabrics;
  final List<OptionAdjustmentDto> cuts;
  final List<SizeDto> sizes;
  final List<PrintingOptionDto> printingOptions;
  final EmbroideryPricingDto? embroideryPricing;

  factory ProductConfigDto.fromJson(Map<String, dynamic> json) {
    List<T> mapList<T>(String key, T Function(Map<String, dynamic>) fromJson) {
      return (json[key] as List<dynamic>)
          .map((e) => fromJson(e as Map<String, dynamic>))
          .toList();
    }

    return ProductConfigDto(
      id: json['id'] as String,
      name: json['name'] as String,
      slug: json['slug'] as String,
      description: json['description'] as String?,
      thumbnailUrl: json['thumbnail_url'] as String?,
      basePrice: (json['base_price'] as num).toDouble(),
      measurementUnit: json['measurement_unit'] as String,
      productTypeName: json['product_type_name'] as String,
      surfaces: mapList('surfaces', SurfaceDto.fromJson),
      fabrics: mapList('fabrics', OptionAdjustmentDto.fromJson),
      cuts: mapList('cuts', OptionAdjustmentDto.fromJson),
      sizes: mapList('sizes', SizeDto.fromJson),
      printingOptions: mapList('printing_options', PrintingOptionDto.fromJson),
      embroideryPricing: json['embroidery_pricing'] == null
          ? null
          : EmbroideryPricingDto.fromJson(json['embroidery_pricing'] as Map<String, dynamic>),
    );
  }
}
