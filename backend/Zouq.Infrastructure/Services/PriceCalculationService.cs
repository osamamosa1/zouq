using Microsoft.EntityFrameworkCore;
using Zouq.Application.Interfaces;
using Zouq.Domain.Enums;
using Zouq.Infrastructure.Data;

namespace Zouq.Infrastructure.Services;

/// <summary>
/// Authoritative price calculation. Never trust client-sent totals.
/// Printing = configured option price. Embroidery = area × price/cm².
/// </summary>
public class PriceCalculationService : IPriceCalculationService
{
    private readonly ZouqDbContext _db;

    public PriceCalculationService(ZouqDbContext db) => _db = db;

    public async Task<PriceBreakdown> CalculateDesignPriceAsync(PriceCalculationRequest request, CancellationToken ct = default)
    {
        var product = await _db.Products
            .Include(p => p.EmbroideryPricing)
            .Include(p => p.PrintingOptions)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && !p.IsDeleted, ct)
            ?? throw new InvalidOperationException("Product not found.");

        if (product.Status != EntityStatus.Active)
            throw new InvalidOperationException("Product is not available.");

        var lines = new List<PriceLine>
        {
            new("base", "Base product", product.BasePrice)
        };

        decimal fabricAdj = 0;
        if (request.FabricId is Guid fabricId)
        {
            var link = await _db.ProductFabrics
                .Include(x => x.Fabric)
                .FirstOrDefaultAsync(x => x.ProductId == product.Id && x.FabricId == fabricId && !x.IsDeleted, ct)
                ?? throw new InvalidOperationException("Fabric is not available for this product.");
            fabricAdj = link.Fabric.PriceAdjustment;
            lines.Add(new("fabric", link.Fabric.Name, fabricAdj));
        }

        decimal cutAdj = 0;
        if (request.CutStyleId is Guid cutId)
        {
            var link = await _db.ProductCuts
                .Include(x => x.CutStyle)
                .FirstOrDefaultAsync(x => x.ProductId == product.Id && x.CutStyleId == cutId && !x.IsDeleted, ct)
                ?? throw new InvalidOperationException("Cut/style is not available for this product.");
            cutAdj = link.CutStyle.PriceAdjustment;
            lines.Add(new("cut", link.CutStyle.Name, cutAdj));
        }

        decimal sizeAdj = 0;
        if (request.ProductSizeId is Guid sizeId)
        {
            var size = await _db.ProductSizes
                .FirstOrDefaultAsync(x => x.ProductId == product.Id && x.Id == sizeId && !x.IsDeleted, ct)
                ?? throw new InvalidOperationException("Size is not available for this product.");
            sizeAdj = size.PriceAdjustment;
            lines.Add(new("size", size.Name, sizeAdj));
        }

        decimal printingCost = 0;
        if (request.PrintingOptionId is Guid printId)
        {
            var option = product.PrintingOptions.FirstOrDefault(o => o.Id == printId && !o.IsDeleted && o.Status == EntityStatus.Active)
                ?? throw new InvalidOperationException("Printing option not found.");

            var usedSurfaces = request.Elements
                .Select(e => e.SurfaceCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var surface in usedSurfaces)
            {
                if (!option.IncludedSurfaceCodes.Any(s => s.Equals(surface, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"Surface '{surface}' is not included in printing option '{option.Name}'.");
            }

            printingCost = option.Price;
            lines.Add(new("printing", option.Name, printingCost));
        }

        decimal embroideryCost = 0;
        var embroideryElements = request.Elements
            .Where(e => e.ProductionMethod == ProductionMethodCode.Embroidery)
            .ToList();

        if (embroideryElements.Count > 0)
        {
            var pricing = product.EmbroideryPricing
                ?? throw new InvalidOperationException("Embroidery pricing is not configured for this product.");

            if (pricing.Status != EntityStatus.Active)
                throw new InvalidOperationException("Embroidery pricing is inactive.");

            foreach (var el in embroideryElements)
            {
                if (el.RealWidth <= 0 || el.RealHeight <= 0)
                    throw new InvalidOperationException("Embroidery elements require positive real width and height.");

                var area = el.RealWidth * el.RealHeight;
                var cost = area * pricing.PricePerSquareUnit;
                if (pricing.MinimumCharge is decimal min && cost < min)
                    cost = min;

                embroideryCost += cost;
                lines.Add(new("embroidery", $"Embroidery {el.SurfaceCode} ({el.RealWidth}×{el.RealHeight})", cost));
            }
        }

        var total = product.BasePrice + fabricAdj + cutAdj + sizeAdj + printingCost + embroideryCost;
        return new PriceBreakdown(
            product.BasePrice,
            fabricAdj,
            cutAdj,
            sizeAdj,
            printingCost,
            embroideryCost,
            total,
            lines);
    }
}
