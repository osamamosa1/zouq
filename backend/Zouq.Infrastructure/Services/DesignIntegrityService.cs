using Microsoft.EntityFrameworkCore;
using Zouq.Domain.Designs;
using Zouq.Domain.Entities;
using Zouq.Domain.Enums;
using Zouq.Infrastructure.Data;

namespace Zouq.Infrastructure.Services;

/// <summary>
/// Server-side design/order integrity: catalog membership, design-area bounds,
/// and authoritative real-world sizes from normalized coordinates (client RealWidth ignored).
/// </summary>
public class DesignIntegrityService
{
    private const double Epsilon = 1e-9;
    private readonly ZouqDbContext _db;

    public DesignIntegrityService(ZouqDbContext db) => _db = db;

    public async Task ValidateAndNormalizeDesignAsync(Design design, CancellationToken ct = default)
    {
        DesignLifecycleRules.EnsureEditable(design.Status);

        var product = await _db.Products
            .Include(p => p.Surfaces).ThenInclude(s => s.DesignArea)
            .Include(p => p.ProductFabrics).ThenInclude(f => f.Fabric)
            .Include(p => p.ProductCuts).ThenInclude(c => c.CutStyle)
            .Include(p => p.Sizes)
            .Include(p => p.PrintingOptions)
            .FirstOrDefaultAsync(p => p.Id == design.ProductId && !p.IsDeleted, ct)
            ?? throw new InvalidOperationException("Product not found.");

        if (product.Status != EntityStatus.Active)
            throw new InvalidOperationException("Product is not available.");

        await ValidateCatalogOptionsAsync(product, design.FabricId, design.CutStyleId, design.ProductSizeId, design.PrintingOptionId, design.Elements, ct);
        NormalizeElementGeometry(product, design.Elements);
    }

    public async Task ValidateCatalogOptionsAsync(
        Product product,
        Guid? fabricId,
        Guid? cutStyleId,
        Guid? productSizeId,
        Guid? printingOptionId,
        IEnumerable<DesignElement> elements,
        CancellationToken ct = default)
    {
        if (fabricId is Guid fid)
        {
            var ok = product.ProductFabrics.Any(x => !x.IsDeleted && x.FabricId == fid && x.Fabric.Status == EntityStatus.Active && !x.Fabric.IsDeleted);
            if (!ok)
                throw new InvalidOperationException("Fabric is not available for this product.");
        }

        if (cutStyleId is Guid cid)
        {
            var ok = product.ProductCuts.Any(x => !x.IsDeleted && x.CutStyleId == cid && x.CutStyle.Status == EntityStatus.Active && !x.CutStyle.IsDeleted);
            if (!ok)
                throw new InvalidOperationException("Cut/style is not available for this product.");
        }

        if (productSizeId is Guid sid)
        {
            var ok = product.Sizes.Any(x => !x.IsDeleted && x.Id == sid && x.Status == EntityStatus.Active);
            if (!ok)
                throw new InvalidOperationException("Size is not available for this product.");
        }

        PrintingOption? printOption = null;
        if (printingOptionId is Guid pid)
        {
            printOption = product.PrintingOptions.FirstOrDefault(o => o.Id == pid && !o.IsDeleted && o.Status == EntityStatus.Active)
                ?? throw new InvalidOperationException("Printing option is not available for this product.");
        }

        var surfaceMap = product.Surfaces
            .Where(s => !s.IsDeleted && s.Status == EntityStatus.Active)
            .ToDictionary(s => s.Code, s => s, StringComparer.OrdinalIgnoreCase);

        foreach (var el in elements.Where(e => !e.IsDeleted))
        {
            if (!surfaceMap.TryGetValue(el.SurfaceCode, out var surface))
                throw new InvalidOperationException($"Surface '{el.SurfaceCode}' is not available for this product.");

            if (printOption != null &&
                !printOption.IncludedSurfaceCodes.Any(s => s.Equals(el.SurfaceCode, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Surface '{el.SurfaceCode}' is not included in printing option '{printOption.Name}'.");
            }

            if (surface.DesignArea == null)
                throw new InvalidOperationException($"Surface '{el.SurfaceCode}' has no design area configured.");
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Overwrites RealWidth/RealHeight from norms × design-area real dimensions.
    /// Rejects elements whose normalized rect is outside the design area (0..1).
    /// </summary>
    public void NormalizeElementGeometry(Product product, IEnumerable<DesignElement> elements)
    {
        var surfaceMap = product.Surfaces
            .Where(s => !s.IsDeleted && s.Status == EntityStatus.Active)
            .ToDictionary(s => s.Code, s => s, StringComparer.OrdinalIgnoreCase);

        foreach (var el in elements.Where(e => !e.IsDeleted))
        {
            if (!surfaceMap.TryGetValue(el.SurfaceCode, out var surface) || surface.DesignArea == null)
                throw new InvalidOperationException($"Surface '{el.SurfaceCode}' has no design area configured.");

            EnsureInsideDesignArea(el);

            var area = surface.DesignArea;
            // Authoritative recompute — ignore any client-provided RealWidth/RealHeight
            el.RealWidth = Round4((decimal)el.NormWidth * area.RealWidth);
            el.RealHeight = Round4((decimal)el.NormHeight * area.RealHeight);

            if (el.RealWidth <= 0 || el.RealHeight <= 0)
                throw new InvalidOperationException("Design element real dimensions must be positive.");
        }
    }

    public static void EnsureInsideDesignArea(DesignElement el)
    {
        if (el.NormWidth <= 0 || el.NormHeight <= 0)
            throw new InvalidOperationException("Design element must have positive normalized width and height.");

        if (el.NormX < -Epsilon || el.NormY < -Epsilon)
            throw new InvalidOperationException("Design element is outside the design area.");

        if (el.NormX + el.NormWidth > 1 + Epsilon || el.NormY + el.NormHeight > 1 + Epsilon)
            throw new InvalidOperationException("Design element is outside the design area.");

        if (el.NormWidth > 1 + Epsilon || el.NormHeight > 1 + Epsilon)
            throw new InvalidOperationException("Design element is outside the design area.");
    }

    public async Task EnsureDesignNotLockedAsync(Guid designId, CancellationToken ct = default)
    {
        var design = await _db.Designs.AsNoTracking().FirstOrDefaultAsync(d => d.Id == designId && !d.IsDeleted, ct)
            ?? throw new InvalidOperationException("Design not found.");
        DesignLifecycleRules.EnsureEditable(design.Status);
    }

    private static decimal Round4(decimal v) => Math.Round(v, 4, MidpointRounding.AwayFromZero);
}
