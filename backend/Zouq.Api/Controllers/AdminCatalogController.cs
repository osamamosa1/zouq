using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zouq.Domain.Common;
using Zouq.Domain.Entities;
using Zouq.Domain.Enums;
using Zouq.Infrastructure.Data;

namespace Zouq.Api.Controllers;

/// <summary>Admin catalog CRUD — products, surfaces, design areas, fabrics, cuts, sizes, printing, embroidery.</summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/catalog")]
public class AdminCatalogController : ControllerBase
{
    private readonly ZouqDbContext _db;
    public AdminCatalogController(ZouqDbContext db) => _db = db;

    // ---- Product types ----
    [HttpGet("product-types")]
    public async Task<ActionResult<ApiResponse<object>>> ListTypes(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _db.ProductTypes.AsNoTracking().Where(t => !t.IsDeleted)
            .OrderBy(t => t.SortOrder).Select(t => new { t.Id, t.Name, t.Slug, t.Status, t.Description }).ToListAsync(ct)));

    [HttpPost("product-types")]
    public async Task<ActionResult<ApiResponse<object>>> CreateType([FromBody] NameSlugBody body, CancellationToken ct)
    {
        var e = new ProductType { Name = body.Name, Slug = body.Slug ?? Slugify(body.Name), Description = body.Description, Status = EntityStatus.Active };
        _db.ProductTypes.Add(e);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { e.Id, e.Name, e.Slug }));
    }

    // ---- Products ----
    [HttpPost("products")]
    public async Task<ActionResult<ApiResponse<object>>> CreateProduct([FromBody] ProductBody body, CancellationToken ct)
    {
        if (!await _db.ProductTypes.AnyAsync(t => t.Id == body.ProductTypeId && !t.IsDeleted, ct))
            return BadRequest(ApiResponse<object>.Fail("Product type not found."));
        var e = new Product
        {
            ProductTypeId = body.ProductTypeId,
            Name = body.Name,
            Slug = body.Slug ?? Slugify(body.Name),
            Description = body.Description,
            BasePrice = body.BasePrice,
            ThumbnailUrl = body.ThumbnailUrl,
            MeasurementUnit = Enum.TryParse<MeasurementUnit>(body.MeasurementUnit, true, out var u) ? u : MeasurementUnit.Centimeter,
            Status = EntityStatus.Active
        };
        _db.Products.Add(e);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { e.Id, e.Name, e.Slug }));
    }

    [HttpPut("products/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateProduct(Guid id, [FromBody] ProductBody body, CancellationToken ct)
    {
        var e = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new InvalidOperationException("Product not found.");
        e.Name = body.Name;
        e.Description = body.Description;
        e.BasePrice = body.BasePrice;
        e.ThumbnailUrl = body.ThumbnailUrl;
        if (!string.IsNullOrWhiteSpace(body.Slug)) e.Slug = body.Slug;
        if (Enum.TryParse<EntityStatus>(body.Status, true, out var st)) e.Status = st;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { e.Id, e.Status }));
    }

    [HttpPost("products/{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateProduct(Guid id, CancellationToken ct)
    {
        var e = await _db.Products.FirstAsync(p => p.Id == id, ct);
        e.Status = EntityStatus.Inactive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { e.Id, e.Status }));
    }

    // ---- Surfaces + design areas ----
    [HttpPost("products/{productId:guid}/surfaces")]
    public async Task<ActionResult<ApiResponse<object>>> AddSurface(Guid productId, [FromBody] SurfaceBody body, CancellationToken ct)
    {
        var surface = new ProductSurface
        {
            ProductId = productId,
            Code = body.Code.Trim().ToLowerInvariant(),
            Name = body.Name,
            PreviewImageUrl = body.PreviewImageUrl,
            SortOrder = body.SortOrder,
            IsRequired = body.IsRequired,
            Status = EntityStatus.Active,
            DesignArea = body.DesignArea == null ? null : new ProductDesignArea
            {
                NormX = body.DesignArea.NormX,
                NormY = body.DesignArea.NormY,
                NormWidth = body.DesignArea.NormWidth,
                NormHeight = body.DesignArea.NormHeight,
                RealWidth = body.DesignArea.RealWidth,
                RealHeight = body.DesignArea.RealHeight
            }
        };
        _db.ProductSurfaces.Add(surface);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { surface.Id, surface.Code }));
    }

    [HttpPut("surfaces/{id:guid}/design-area")]
    public async Task<ActionResult<ApiResponse<object>>> UpsertDesignArea(Guid id, [FromBody] DesignAreaBody body, CancellationToken ct)
    {
        var surface = await _db.ProductSurfaces.Include(s => s.DesignArea)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct)
            ?? throw new InvalidOperationException("Surface not found.");
        if (surface.DesignArea == null)
        {
            surface.DesignArea = new ProductDesignArea { ProductSurfaceId = surface.Id };
            _db.ProductDesignAreas.Add(surface.DesignArea);
        }
        surface.DesignArea.NormX = body.NormX;
        surface.DesignArea.NormY = body.NormY;
        surface.DesignArea.NormWidth = body.NormWidth;
        surface.DesignArea.NormHeight = body.NormHeight;
        surface.DesignArea.RealWidth = body.RealWidth;
        surface.DesignArea.RealHeight = body.RealHeight;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { surface.Id, surface.DesignArea.RealWidth, surface.DesignArea.RealHeight }));
    }

    [HttpGet("products/{productId:guid}/full")]
    public async Task<ActionResult<ApiResponse<object>>> ProductFull(Guid productId, CancellationToken ct)
    {
        var p = await _db.Products.AsNoTracking()
            .Include(x => x.ProductType)
            .Include(x => x.Surfaces).ThenInclude(s => s.DesignArea)
            .Include(x => x.ProductFabrics).ThenInclude(f => f.Fabric)
            .Include(x => x.ProductCuts).ThenInclude(c => c.CutStyle)
            .Include(x => x.Sizes)
            .Include(x => x.PrintingOptions)
            .Include(x => x.EmbroideryPricing)
            .FirstOrDefaultAsync(x => x.Id == productId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Product not found.");
        return Ok(ApiResponse<object>.Ok(p));
    }

    [HttpPut("commission")]
    public async Task<ActionResult<ApiResponse<object>>> SetCommission([FromBody] CommissionBody body, CancellationToken ct)
    {
        var setting = await _db.CommissionSettings.FirstOrDefaultAsync(c => c.IsActive && !c.IsDeleted, ct);
        if (setting == null)
        {
            setting = new CommissionSetting { Name = "default", IsActive = true };
            _db.CommissionSettings.Add(setting);
        }
        setting.DesignerCommissionPercent = body.DesignerCommissionPercent;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { setting.Id, setting.DesignerCommissionPercent }));
    }

    [HttpGet("commission")]
    public async Task<ActionResult<ApiResponse<object>>> GetCommission(CancellationToken ct)
    {
        var setting = await _db.CommissionSettings.AsNoTracking()
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .FirstOrDefaultAsync(ct);
        return Ok(ApiResponse<object>.Ok(setting == null
            ? new { designer_commission_percent = 0m }
            : new { setting.Id, designer_commission_percent = setting.DesignerCommissionPercent }));
    }

    [HttpGet("fabrics")]
    public async Task<ActionResult<ApiResponse<object>>> ListFabrics(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _db.Fabrics.AsNoTracking().Where(f => !f.IsDeleted)
            .OrderBy(f => f.Name).Select(f => new { f.Id, f.Name, f.PriceAdjustment, f.Status, f.ImageUrl }).ToListAsync(ct)));

    [HttpPost("fabrics/{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateFabric(Guid id, CancellationToken ct)
    {
        var e = await _db.Fabrics.FirstAsync(f => f.Id == id, ct);
        e.Status = EntityStatus.Inactive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { e.Id, e.Status }));
    }

    [HttpGet("cuts")]
    public async Task<ActionResult<ApiResponse<object>>> ListCuts(CancellationToken ct) =>
        Ok(ApiResponse<object>.Ok(await _db.CutStyles.AsNoTracking().Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name).Select(c => new { c.Id, c.Name, c.PriceAdjustment, c.Status }).ToListAsync(ct)));

    [HttpPost("cuts/{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateCut(Guid id, CancellationToken ct)
    {
        var e = await _db.CutStyles.FirstAsync(c => c.Id == id, ct);
        e.Status = EntityStatus.Inactive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { e.Id, e.Status }));
    }

    [HttpPost("products/{productId:guid}/sizes/{sizeId:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateSize(Guid productId, Guid sizeId, CancellationToken ct)
    {
        var e = await _db.ProductSizes.FirstAsync(s => s.Id == sizeId && s.ProductId == productId, ct);
        e.Status = EntityStatus.Inactive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { e.Id, e.Status }));
    }

    private static string Slugify(string name) =>
        string.Join('-', name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries))
            + "-" + Guid.NewGuid().ToString("N")[..6];

    public record NameSlugBody(string Name, string? Slug, string? Description);
    public record ProductBody(Guid ProductTypeId, string Name, string? Slug, string? Description, decimal BasePrice, string? ThumbnailUrl, string? MeasurementUnit, string? Status);
    public record SurfaceBody(string Code, string Name, string? PreviewImageUrl, int SortOrder, bool IsRequired, DesignAreaBody? DesignArea);
    public record DesignAreaBody(double NormX, double NormY, double NormWidth, double NormHeight, decimal RealWidth, decimal RealHeight);
    public record CommissionBody(decimal DesignerCommissionPercent);
}
