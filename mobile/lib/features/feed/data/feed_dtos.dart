class FeedItemDto {
  FeedItemDto({
    required this.designId,
    required this.title,
    this.previewImageUrl,
    required this.ownerName,
    required this.isFeatured,
    required this.featuredPriority,
    required this.tags,
    this.priceHint,
  });

  final String designId;
  final String title;
  final String? previewImageUrl;
  final String ownerName;
  final bool isFeatured;
  final int featuredPriority;
  final List<String> tags;
  final double? priceHint;

  factory FeedItemDto.fromJson(Map<String, dynamic> json) {
    return FeedItemDto(
      designId: json['design_id'] as String,
      title: json['title'] as String,
      previewImageUrl: json['preview_image_url'] as String?,
      ownerName: json['owner_name'] as String,
      isFeatured: json['is_featured'] as bool? ?? false,
      featuredPriority: (json['featured_priority'] as num?)?.toInt() ?? 0,
      tags: (json['tags'] as List<dynamic>?)?.map((e) => e as String).toList() ?? const [],
      priceHint: json['price_hint'] == null ? null : (json['price_hint'] as num).toDouble(),
    );
  }
}

class AdDto {
  AdDto({
    required this.id,
    required this.title,
    this.description,
    this.imageUrl,
    this.linkUrl,
    this.actionType,
    required this.placement,
    required this.displayPriority,
  });

  final String id;
  final String title;
  final String? description;
  final String? imageUrl;
  final String? linkUrl;
  final String? actionType;
  final String placement;
  final int displayPriority;

  factory AdDto.fromJson(Map<String, dynamic> json) {
    return AdDto(
      id: json['id'] as String,
      title: json['title'] as String,
      description: json['description'] as String?,
      imageUrl: json['image_url'] as String?,
      linkUrl: json['link_url'] as String?,
      actionType: json['action_type'] as String?,
      placement: json['placement'] as String,
      displayPriority: (json['display_priority'] as num).toInt(),
    );
  }
}
