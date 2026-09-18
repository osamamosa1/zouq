namespace Zouq.Domain.Enums;

public enum UserRole
{
    Customer = 0,
    Admin = 1
}

public enum EntityStatus
{
    Draft = 0,
    Active = 1,
    Inactive = 2,
    Archived = 3
}

public enum DesignStatus
{
    /// <summary>Private working draft during customization. Not in For You.</summary>
    Draft = 0,

    /// <summary>Legacy/manual publish flag — prefer Reusable after Delivered.</summary>
    Published = 1,

    Unpublished = 2,

    /// <summary>Attached to an order; immutable. Not public.</summary>
    Ordered = 3,

    /// <summary>Related order reached Delivered — eligible to become reusable if owner publishes.</summary>
    DeliveredEligible = 4,

    /// <summary>Public/reusable in For You. Only reachable from DeliveredEligible.</summary>
    Reusable = 5,

    Archived = 6,

    /// <summary>Backward-compatible alias of Ordered.</summary>
    UsedInOrder = Ordered
}

public enum DesignVisibility
{
    Private = 0,
    Public = 1,
    Unlisted = 2
}

public enum ProductionMethodCode
{
    Printing = 0,
    Embroidery = 1
}

/// <summary>
/// Canonical order lifecycle. Confirmed is retained as an alias of Processing for API compatibility.
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    InProduction = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5,
    Refunded = 6,

    /// <summary>Legacy alias — same value as Processing.</summary>
    Confirmed = Processing
}

public enum LedgerEntryType
{
    Credit = 0,
    Debit = 1
}

public enum LedgerTransactionType
{
    CreatorReward = 0,
    CreatorRewardReversal = 1,
    AdminAdjustment = 2
}

public enum LedgerEntryStatus
{
    Posted = 0
}

public enum CreatorRewardStatus
{
    Credited = 0,
    Reversed = 1
}

public enum AdPlacement
{
    Banner = 0,
    Popup = 1,
    Feed = 2,
    Splash = 3
}

public enum MeasurementUnit
{
    Centimeter = 0,
    Inch = 1,
    Millimeter = 2
}
