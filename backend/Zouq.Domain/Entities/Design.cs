using Zouq.Domain.Common;
using Zouq.Domain.Enums;

namespace Zouq.Domain.Entities;

public class DesignAssetCategory : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public ICollection<DesignAsset> Assets { get; set; } = new List<DesignAsset>();
}

public class DesignAsset : AuditableEntity
{
    public Guid? CategoryId { get; set; }
    public DesignAssetCategory? Category { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<Guid> CompatibleProductIds { get; set; } = new();
    public List<string> CompatibleSurfaceCodes { get; set; } = new();
}

public class UserUpload : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
    public List<string> AiTags { get; set; } = new();
    public string? AiAnalysisStatus { get; set; } = "pending";
    public string? AiProvider { get; set; }
    public string? AiModel { get; set; }
    public string? AiError { get; set; }
    public DateTime? AiProcessedAtUtc { get; set; }
}

/// <summary>
/// Saved user design. Mutable until ordered. Public/reusable only after related order is Delivered
/// and the owner (or admin) explicitly publishes it.
/// </summary>
public class Design : AuditableEntity
{
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid? FabricId { get; set; }
    public Fabric? Fabric { get; set; }
    public Guid? CutStyleId { get; set; }
    public CutStyle? CutStyle { get; set; }
    public Guid? ProductSizeId { get; set; }
    public ProductSize? ProductSize { get; set; }
    public Guid? PrintingOptionId { get; set; }
    public PrintingOption? PrintingOption { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PreviewImageUrl { get; set; }
    public DesignStatus Status { get; set; } = DesignStatus.Draft;
    public DesignVisibility Visibility { get; set; } = DesignVisibility.Private;
    public bool IsFeatured { get; set; }
    public int FeaturedPriority { get; set; }
    public List<string> Tags { get; set; } = new();
    public decimal? LastEstimatedPrice { get; set; }

    /// <summary>Order that made this design ordered / eligible (source of truth for publication gate).</summary>
    public Guid? SourceOrderId { get; set; }
    public Order? SourceOrder { get; set; }

    /// <summary>
    /// When this draft was cloned from a published reusable design, points at that source.
    /// Creator attribution for orders uses the source owner's Id — never mutates the source.
    /// </summary>
    public Guid? DerivedFromDesignId { get; set; }
    public Design? DerivedFromDesign { get; set; }

    public DateTime? BecameEligibleAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }

    public ICollection<DesignElement> Elements { get; set; } = new List<DesignElement>();
}

public class DesignElement : AuditableEntity
{
    public Guid DesignId { get; set; }
    public Design Design { get; set; } = null!;
    public string SurfaceCode { get; set; } = string.Empty;

    public Guid? DesignAssetId { get; set; }
    public DesignAsset? DesignAsset { get; set; }
    public Guid? UserUploadId { get; set; }
    public UserUpload? UserUpload { get; set; }

    public ProductionMethodCode ProductionMethod { get; set; } = ProductionMethodCode.Printing;

    // Normalized geometry relative to the surface design area (0..1)
    public double NormX { get; set; }
    public double NormY { get; set; }
    public double NormWidth { get; set; }
    public double NormHeight { get; set; }
    public double RotationDegrees { get; set; }
    public double Scale { get; set; } = 1;

    // Real-world size in product measurement units (authoritative for embroidery pricing)
    public decimal RealWidth { get; set; }
    public decimal RealHeight { get; set; }

    public int ZIndex { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}
