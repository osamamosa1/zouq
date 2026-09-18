class OrderDto {
  OrderDto({
    required this.id,
    required this.orderNumber,
    required this.status,
    required this.subtotal,
    required this.total,
    required this.currency,
    required this.createdAtUtc,
    required this.items,
  });

  final String id;
  final String orderNumber;
  final String status;
  final double subtotal;
  final double total;
  final String currency;
  final DateTime createdAtUtc;
  final List<OrderItemDto> items;

  factory OrderDto.fromJson(Map<String, dynamic> json) {
    return OrderDto(
      id: json['id'] as String,
      orderNumber: json['order_number'] as String,
      status: json['status'] as String,
      subtotal: (json['subtotal'] as num).toDouble(),
      total: (json['total'] as num).toDouble(),
      currency: json['currency'] as String,
      createdAtUtc: DateTime.parse(json['created_at_utc'] as String),
      items: (json['items'] as List<dynamic>)
          .map((e) => OrderItemDto.fromJson(e as Map<String, dynamic>))
          .toList(),
    );
  }
}

class OrderItemDto {
  OrderItemDto({
    required this.id,
    this.sourceDesignId,
    required this.quantity,
    required this.unitPrice,
    required this.lineTotal,
  });

  final String id;
  final String? sourceDesignId;
  final int quantity;
  final double unitPrice;
  final double lineTotal;

  factory OrderItemDto.fromJson(Map<String, dynamic> json) {
    return OrderItemDto(
      id: json['id'] as String,
      sourceDesignId: json['source_design_id'] as String?,
      quantity: (json['quantity'] as num).toInt(),
      unitPrice: (json['unit_price'] as num).toDouble(),
      lineTotal: (json['line_total'] as num).toDouble(),
    );
  }
}

class OrderDetailDto {
  OrderDetailDto({
    required this.id,
    required this.orderNumber,
    required this.status,
    required this.subtotal,
    required this.total,
    required this.currency,
    required this.createdAtUtc,
    required this.commissionPercentSnapshot,
    this.creatorRewardAmount,
    required this.items,
  });

  final String id;
  final String orderNumber;
  final String status;
  final double subtotal;
  final double total;
  final String currency;
  final DateTime createdAtUtc;
  final double commissionPercentSnapshot;
  final double? creatorRewardAmount;
  final List<OrderDetailItemDto> items;

  factory OrderDetailDto.fromJson(Map<String, dynamic> json) {
    return OrderDetailDto(
      id: json['id'] as String,
      orderNumber: json['order_number'] as String,
      status: json['status'] as String,
      subtotal: (json['subtotal'] as num).toDouble(),
      total: (json['total'] as num).toDouble(),
      currency: json['currency'] as String,
      createdAtUtc: DateTime.parse(json['created_at_utc'] as String),
      commissionPercentSnapshot: (json['commission_percent_snapshot'] as num?)?.toDouble() ?? 0,
      creatorRewardAmount: (json['creator_reward_amount'] as num?)?.toDouble(),
      items: (json['items'] as List<dynamic>)
          .map((e) => OrderDetailItemDto.fromJson(Map<String, dynamic>.from(e as Map)))
          .toList(),
    );
  }
}

class OrderDetailItemDto {
  OrderDetailItemDto({
    required this.id,
    this.sourceDesignId,
    this.designerId,
    required this.quantity,
    required this.unitPrice,
    required this.lineTotal,
    required this.designSnapshotJson,
    required this.pricingBreakdownJson,
    required this.productSnapshotJson,
  });

  final String id;
  final String? sourceDesignId;
  final String? designerId;
  final int quantity;
  final double unitPrice;
  final double lineTotal;
  final String designSnapshotJson;
  final String pricingBreakdownJson;
  final String productSnapshotJson;

  factory OrderDetailItemDto.fromJson(Map<String, dynamic> json) {
    return OrderDetailItemDto(
      id: json['id'] as String,
      sourceDesignId: json['source_design_id'] as String?,
      designerId: json['designer_id'] as String?,
      quantity: (json['quantity'] as num).toInt(),
      unitPrice: (json['unit_price'] as num).toDouble(),
      lineTotal: (json['line_total'] as num).toDouble(),
      designSnapshotJson: json['design_snapshot_json'] as String? ?? '{}',
      pricingBreakdownJson: json['pricing_breakdown_json'] as String? ?? '{}',
      productSnapshotJson: json['product_snapshot_json'] as String? ?? '{}',
    );
  }
}
