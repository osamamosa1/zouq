using Zouq.Domain.Common;
using Zouq.Domain.Enums;

namespace Zouq.Domain.Entities;

/// <summary>
/// Printing is priced by configured option (e.g. Front only / Front+Back), NOT per cm².
/// </summary>
public class PrintingOption : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public List<string> IncludedSurfaceCodes { get; set; } = new();
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public int SortOrder { get; set; }
}

/// <summary>
/// Embroidery is priced by area: RealWidth × RealHeight × PricePerSquareUnit.
/// </summary>
public class EmbroideryPricing : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public decimal PricePerSquareUnit { get; set; }
    public MeasurementUnit Unit { get; set; } = MeasurementUnit.Centimeter;
    public decimal? MinimumCharge { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
}

public class CommissionSetting : AuditableEntity
{
    public string Name { get; set; } = "default";
    public decimal DesignerCommissionPercent { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Order : AuditableEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid BuyerId { get; set; }
    public User Buyer { get; set; } = null!;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "EGP";
    public string? ShippingAddressJson { get; set; }
    public string? Notes { get; set; }

    /// <summary>Commission % frozen at order creation — never follow live CommissionSettings.</summary>
    public decimal CommissionPercentSnapshot { get; set; }

    /// <summary>Persisted when reward becomes eligible/credited; null until Delivered reward runs.</summary>
    public decimal? CreatorRewardAmount { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<CreatorReward> CreatorRewards { get; set; } = new List<CreatorReward>();
}

/// <summary>
/// Immutable snapshot of the purchased design/configuration and price breakdown.
/// Future catalog/price changes MUST NOT affect this row.
/// </summary>
public class OrderItem : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid? SourceDesignId { get; set; }
    public Design? SourceDesign { get; set; }
    public Guid? DesignerId { get; set; }
    public User? Designer { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }

    public string DesignSnapshotJson { get; set; } = "{}";
    public string PricingBreakdownJson { get; set; } = "{}";
    public string ProductSnapshotJson { get; set; } = "{}";
}

/// <summary>
/// Exactly one credited reward row per (Order, Creator). DB unique index enforces idempotency.
/// </summary>
public class CreatorReward : AuditableEntity
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid? OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }
    public Guid CreatorId { get; set; }
    public User Creator { get; set; } = null!;
    public Guid? DesignId { get; set; }
    public Design? Design { get; set; }

    public decimal CommissionPercent { get; set; }
    public decimal Amount { get; set; }
    public CreatorRewardStatus Status { get; set; } = CreatorRewardStatus.Credited;

    public Guid CreditLedgerEntryId { get; set; }
    public LedgerEntry CreditLedgerEntry { get; set; } = null!;
    public Guid? ReversalLedgerEntryId { get; set; }
    public LedgerEntry? ReversalLedgerEntry { get; set; }
}

/// <summary>
/// Append-only financial journal. Never delete rows to "fix" balances — reverse instead.
/// </summary>
public class LedgerEntry : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public LedgerEntryType EntryType { get; set; }
    public LedgerTransactionType TransactionType { get; set; }
    public LedgerEntryStatus Status { get; set; } = LedgerEntryStatus.Posted;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Reason { get; set; } = string.Empty;

    /// <summary>Globally unique idempotency key (e.g. creator-reward:{orderId}:{creatorId}).</summary>
    public string Reference { get; set; } = string.Empty;

    public decimal? CommissionPercent { get; set; }
    public Guid? OrderId { get; set; }
    public Order? Order { get; set; }
    public Guid? OrderItemId { get; set; }
    public OrderItem? OrderItem { get; set; }
    public Guid? DesignId { get; set; }
    public Design? Design { get; set; }
    public Guid? CreatorId { get; set; }
    public User? Creator { get; set; }
    public Guid? CreatedByAdminId { get; set; }
    public User? CreatedByAdmin { get; set; }
}

public class Advertisement : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? ActionType { get; set; }
    public AdPlacement Placement { get; set; } = AdPlacement.Banner;
    public DateTime? StartAtUtc { get; set; }
    public DateTime? EndAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayPriority { get; set; }
    public string? TargetingJson { get; set; }
}

public class PlatformSetting : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
