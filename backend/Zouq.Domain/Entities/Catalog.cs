using Zouq.Domain.Common;
using Zouq.Domain.Enums;

namespace Zouq.Domain.Entities;

/// <summary>
/// Generic product catalog — not hardcoded to T-shirts.
/// Admins configure products, surfaces, fabrics, cuts, sizes dynamically.
/// </summary>
public class ProductType : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public int SortOrder { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product : AuditableEntity
{
    public Guid ProductTypeId { get; set; }
    public ProductType ProductType { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public decimal BasePrice { get; set; }
    public MeasurementUnit MeasurementUnit { get; set; } = MeasurementUnit.Centimeter;
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public int SortOrder { get; set; }

    public ICollection<ProductSurface> Surfaces { get; set; } = new List<ProductSurface>();
    public ICollection<ProductFabric> ProductFabrics { get; set; } = new List<ProductFabric>();
    public ICollection<ProductCut> ProductCuts { get; set; } = new List<ProductCut>();
    public ICollection<ProductSize> Sizes { get; set; } = new List<ProductSize>();
    public ICollection<PrintingOption> PrintingOptions { get; set; } = new List<PrintingOption>();
    public EmbroideryPricing? EmbroideryPricing { get; set; }
}

public class ProductSurface : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Code { get; set; } = string.Empty; // front, back, left_sleeve, ...
    public string Name { get; set; } = string.Empty;
    public string? PreviewImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public ProductDesignArea? DesignArea { get; set; }
}

/// <summary>
/// Printable/embroidery region on a surface, in product measurement units.
/// Coordinates are relative to the surface (0–1 normalized) plus real-world bounds.
/// </summary>
public class ProductDesignArea : AuditableEntity
{
    public Guid ProductSurfaceId { get; set; }
    public ProductSurface ProductSurface { get; set; } = null!;

    // Normalized rect on the surface preview (0..1)
    public double NormX { get; set; }
    public double NormY { get; set; }
    public double NormWidth { get; set; }
    public double NormHeight { get; set; }

    // Real-world printable area (same unit as Product.MeasurementUnit)
    public decimal RealWidth { get; set; }
    public decimal RealHeight { get; set; }
}

public class Fabric : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal PriceAdjustment { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
    public Dictionary<string, string> Metadata { get; set; } = new();

    public ICollection<ProductFabric> ProductFabrics { get; set; } = new List<ProductFabric>();
}

public class ProductFabric : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid FabricId { get; set; }
    public Fabric Fabric { get; set; } = null!;
    public bool IsDefault { get; set; }
}

public class CutStyle : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public decimal PriceAdjustment { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;

    public ICollection<ProductCut> ProductCuts { get; set; } = new List<ProductCut>();
}

public class ProductCut : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid CutStyleId { get; set; }
    public CutStyle CutStyle { get; set; } = null!;
    public bool IsDefault { get; set; }
}

public class ProductSize : AuditableEntity
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Code { get; set; } = string.Empty; // S, M, L, XL
    public string Name { get; set; } = string.Empty;
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public decimal? Depth { get; set; }
    public MeasurementUnit Unit { get; set; } = MeasurementUnit.Centimeter;
    public decimal PriceAdjustment { get; set; }
    public int SortOrder { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Active;
}
