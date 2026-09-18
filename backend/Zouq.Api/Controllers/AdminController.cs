using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zouq.Domain.Common;
using Zouq.Domain.Entities;
using Zouq.Domain.Enums;
using Zouq.Infrastructure.Data;
using Zouq.Infrastructure.Services;

namespace Zouq.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly ZouqDbContext _db;
    private readonly OrderAppService _orders;
    private readonly FinancialLedgerService _ledger;

    public AdminController(ZouqDbContext db, OrderAppService orders, FinancialLedgerService ledger)
    {
        _db = db;
        _orders = orders;
        _ledger = ledger;
    }

    private Guid AdminId => Guid.Parse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("orders/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> OrderDetail(Guid id, CancellationToken ct)
    {
        var o = await _db.Orders.AsNoTracking()
            .Include(x => x.Buyer)
            .Include(x => x.Items)
            .Include(x => x.CreatorRewards)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Order not found.");

        var ledger = await _db.LedgerEntries.AsNoTracking()
            .Where(l => l.OrderId == id)
            .OrderBy(l => l.CreatedAtUtc)
            .Select(l => new { l.Id, l.EntryType, l.TransactionType, l.Amount, l.BalanceAfter, l.Reason, l.Reference, l.CreatedAtUtc })
            .ToListAsync(ct);

        return Ok(ApiResponse<object>.Ok(new
        {
            o.Id,
            o.OrderNumber,
            o.Status,
            o.Subtotal,
            o.Total,
            o.Currency,
            o.CommissionPercentSnapshot,
            o.CreatorRewardAmount,
            o.CreatedAtUtc,
            Buyer = new { o.Buyer.Id, o.Buyer.Name, o.Buyer.Email },
            Items = o.Items.Select(i => new
            {
                i.Id,
                i.SourceDesignId,
                i.DesignerId,
                i.Quantity,
                i.UnitPrice,
                i.LineTotal,
                i.DesignSnapshotJson,
                i.ProductSnapshotJson,
                i.PricingBreakdownJson
            }),
            CreatorRewards = o.CreatorRewards.Select(r => new
            {
                r.Id, r.CreatorId, r.DesignId, r.Amount, r.CommissionPercent, r.Status, r.CreatedAtUtc
            }),
            Ledger = ledger
        }));
    }

    [HttpGet("ledger")]
    public async Task<ActionResult<ApiResponse<object>>> Ledger(
        [FromQuery] Guid? userId, [FromQuery] Guid? orderId, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 500);
        var q = _db.LedgerEntries.AsNoTracking().AsQueryable();
        if (userId is Guid uid) q = q.Where(l => l.UserId == uid);
        if (orderId is Guid oid) q = q.Where(l => l.OrderId == oid);
        var list = await q.OrderByDescending(l => l.CreatedAtUtc).Take(take)
            .Select(l => new
            {
                l.Id, l.UserId, l.EntryType, l.TransactionType, l.Amount, l.BalanceAfter,
                l.Reason, l.Reference, l.OrderId, l.CommissionPercent, l.CreatedByAdminId, l.CreatedAtUtc
            }).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(list));
    }

    [HttpGet("creator-rewards")]
    public async Task<ActionResult<ApiResponse<object>>> CreatorRewards(CancellationToken ct)
    {
        var list = await _db.CreatorRewards.AsNoTracking()
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(200)
            .Select(r => new
            {
                r.Id, r.OrderId, r.CreatorId, r.DesignId, r.Amount, r.CommissionPercent, r.Status, r.CreatedAtUtc
            }).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(list));
    }

    [HttpGet("designs")]
    public async Task<ActionResult<ApiResponse<object>>> Designs(
        [FromQuery] string? status, [FromQuery] bool? featured, CancellationToken ct = default)
    {
        var q = _db.Designs.AsNoTracking().Include(d => d.Owner).Where(d => !d.IsDeleted);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DesignStatus>(status, true, out var st))
            q = q.Where(d => d.Status == st);
        if (featured is bool f) q = q.Where(d => d.IsFeatured == f);
        var list = await q.OrderByDescending(d => d.UpdatedAtUtc).Take(200)
            .Select(d => new
            {
                d.Id, d.Title, d.Status, d.Visibility, d.IsFeatured, d.FeaturedPriority,
                d.Tags, d.SourceOrderId, d.BecameEligibleAtUtc, d.PublishedAtUtc,
                Owner = d.Owner.Name, OwnerId = d.OwnerId
            }).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(list));
    }

    [HttpPost("designs/{id:guid}/publish")]
    public async Task<ActionResult<ApiResponse<object>>> AdminPublish(Guid id, CancellationToken ct)
    {
        var d = await _db.Designs.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Design not found.");
        Domain.Designs.DesignLifecycleRules.EnsureCanPublishAsReusable(d.Status);
        d.Status = DesignStatus.Reusable;
        d.Visibility = DesignVisibility.Public;
        d.PublishedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { d.Id, d.Status }));
    }

    [HttpGet("ads")]
    public async Task<ActionResult<ApiResponse<object>>> ListAds(CancellationToken ct)
    {
        var list = await _db.Advertisements.AsNoTracking().Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.DisplayPriority)
            .Select(a => new
            {
                a.Id, a.Title, a.Description, a.ImageUrl, a.LinkUrl, a.ActionType,
                a.Placement, a.StartAtUtc, a.EndAtUtc, a.IsActive, a.DisplayPriority
            }).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(list));
    }

    [HttpPatch("ads/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateAd(Guid id, [FromBody] AdUpdateBody body, CancellationToken ct)
    {
        var ad = await _db.Advertisements.FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted, ct)
            ?? throw new InvalidOperationException("Ad not found.");
        if (body.Title != null) ad.Title = body.Title;
        if (body.Description != null) ad.Description = body.Description;
        if (body.ImageUrl != null) ad.ImageUrl = body.ImageUrl;
        if (body.LinkUrl != null) ad.LinkUrl = body.LinkUrl;
        if (body.IsActive is bool active) ad.IsActive = active;
        if (body.DisplayPriority is int prio) ad.DisplayPriority = prio;
        if (body.StartAtUtc is DateTime s) ad.StartAtUtc = s;
        if (body.EndAtUtc is DateTime e) ad.EndAtUtc = e;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { ad.Id, ad.IsActive }));
    }

    [HttpDelete("ads/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> SoftDeleteAd(Guid id, CancellationToken ct)
    {
        var ad = await _db.Advertisements.FirstAsync(a => a.Id == id, ct);
        ad.IsDeleted = true;
        ad.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { id }));
    }

    [HttpPost("design-assets/{id:guid}/deactivate")]
    public async Task<ActionResult<ApiResponse<object>>> DeactivateAsset(Guid id, CancellationToken ct)
    {
        var asset = await _db.DesignAssets.FirstAsync(a => a.Id == id, ct);
        asset.Status = EntityStatus.Inactive;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { asset.Id, asset.Status }));
    }

    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<object>>> Orders(CancellationToken ct)
    {
        var list = await _db.Orders.AsNoTracking().Include(o => o.Items).Include(o => o.Buyer)
            .OrderByDescending(o => o.CreatedAtUtc)
            .Select(o => new
            {
                o.Id, o.OrderNumber, o.Status, o.Total, o.Currency, o.CreatedAtUtc,
                o.CommissionPercentSnapshot, o.CreatorRewardAmount,
                Buyer = o.Buyer.Name, BuyerEmail = o.Buyer.Email
            }).ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(list));
    }

    [HttpPatch("orders/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> SetOrderStatus(Guid id, [FromBody] StatusBody body, CancellationToken ct)
    {
        if (!Enum.TryParse<OrderStatus>(body.Status, true, out var status))
            return BadRequest(ApiResponse<object>.Fail("Invalid status."));
        var order = await _orders.TransitionStatusAsync(id, status, AdminId, ct);
        return Ok(ApiResponse<object>.Ok(new
        {
            order.Id,
            order.Status,
            order.OrderNumber
        }));
    }

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<object>>> Products(CancellationToken ct)
    {
        var list = await _db.Products.AsNoTracking().Include(p => p.ProductType)
            .Where(p => !p.IsDeleted)
            .Select(p => new { p.Id, p.Name, p.Slug, p.BasePrice, p.Status, Type = p.ProductType.Name })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(list));
    }

    [HttpPost("fabrics")]
    public async Task<ActionResult<ApiResponse<object>>> CreateFabric([FromBody] FabricBody body, CancellationToken ct)
    {
        var fabric = new Fabric
        {
            Name = body.Name,
            Description = body.Description,
            ImageUrl = body.ImageUrl,
            PriceAdjustment = body.PriceAdjustment,
            Status = EntityStatus.Active
        };
        _db.Fabrics.Add(fabric);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { fabric.Id, fabric.Name }));
    }

    [HttpPost("products/{productId:guid}/fabrics/{fabricId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> LinkFabric(Guid productId, Guid fabricId, CancellationToken ct)
    {
        _db.ProductFabrics.Add(new ProductFabric { ProductId = productId, FabricId = fabricId });
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { productId, fabricId }));
    }

    [HttpPost("cuts")]
    public async Task<ActionResult<ApiResponse<object>>> CreateCut([FromBody] CutBody body, CancellationToken ct)
    {
        var cut = new CutStyle
        {
            Name = body.Name,
            Description = body.Description,
            ImageUrl = body.ImageUrl,
            PriceAdjustment = body.PriceAdjustment,
            Status = EntityStatus.Active
        };
        _db.CutStyles.Add(cut);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { cut.Id, cut.Name }));
    }

    [HttpPost("products/{productId:guid}/cuts/{cutId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> LinkCut(Guid productId, Guid cutId, CancellationToken ct)
    {
        _db.ProductCuts.Add(new ProductCut { ProductId = productId, CutStyleId = cutId });
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { productId, cutId }));
    }

    [HttpPost("products/{productId:guid}/sizes")]
    public async Task<ActionResult<ApiResponse<object>>> AddSize(Guid productId, [FromBody] SizeBody body, CancellationToken ct)
    {
        var size = new ProductSize
        {
            ProductId = productId,
            Code = body.Code,
            Name = body.Name,
            Width = body.Width,
            Height = body.Height,
            Depth = body.Depth,
            Unit = Enum.TryParse<MeasurementUnit>(body.Unit, true, out var u) ? u : MeasurementUnit.Centimeter,
            PriceAdjustment = body.PriceAdjustment,
            Status = EntityStatus.Active
        };
        _db.ProductSizes.Add(size);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { size.Id, size.Code }));
    }

    [HttpPut("products/{productId:guid}/embroidery-pricing")]
    public async Task<ActionResult<ApiResponse<object>>> SetEmbroidery(Guid productId, [FromBody] EmbroideryBody body, CancellationToken ct)
    {
        var existing = await _db.EmbroideryPricings.FirstOrDefaultAsync(e => e.ProductId == productId, ct);
        if (existing == null)
        {
            existing = new EmbroideryPricing { ProductId = productId };
            _db.EmbroideryPricings.Add(existing);
        }
        existing.PricePerSquareUnit = body.PricePerSquareUnit;
        existing.MinimumCharge = body.MinimumCharge;
        existing.Status = EntityStatus.Active;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { existing.ProductId, existing.PricePerSquareUnit }));
    }

    [HttpPost("products/{productId:guid}/printing-options")]
    public async Task<ActionResult<ApiResponse<object>>> AddPrinting(Guid productId, [FromBody] PrintingBody body, CancellationToken ct)
    {
        var opt = new PrintingOption
        {
            ProductId = productId,
            Name = body.Name,
            Code = body.Code,
            Description = body.Description,
            Price = body.Price,
            IncludedSurfaceCodes = body.IncludedSurfaceCodes ?? new List<string>(),
            Status = EntityStatus.Active
        };
        _db.PrintingOptions.Add(opt);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { opt.Id, opt.Name, opt.Price }));
    }

    [HttpPost("design-assets")]
    public async Task<ActionResult<ApiResponse<object>>> CreateAsset([FromBody] AssetBody body, CancellationToken ct)
    {
        var asset = new DesignAsset
        {
            Name = body.Name,
            FileUrl = body.FileUrl,
            ThumbnailUrl = body.ThumbnailUrl,
            CategoryId = body.CategoryId,
            Tags = body.Tags ?? new List<string>(),
            CompatibleProductIds = body.CompatibleProductIds ?? new List<Guid>(),
            CompatibleSurfaceCodes = body.CompatibleSurfaceCodes ?? new List<string>(),
            Status = EntityStatus.Active
        };
        _db.DesignAssets.Add(asset);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { asset.Id, asset.Name }));
    }

    [HttpPost("designs/{id:guid}/feature")]
    public async Task<ActionResult<ApiResponse<object>>> FeatureDesign(Guid id, [FromBody] FeatureBody body, CancellationToken ct)
    {
        var design = await _db.Designs.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new InvalidOperationException("Design not found.");
        design.IsFeatured = body.IsFeatured;
        design.FeaturedPriority = body.Priority;
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { design.Id, design.IsFeatured, design.FeaturedPriority }));
    }

    [HttpPost("ads")]
    public async Task<ActionResult<ApiResponse<object>>> CreateAd([FromBody] AdBody body, CancellationToken ct)
    {
        var ad = new Advertisement
        {
            Title = body.Title,
            Description = body.Description,
            ImageUrl = body.ImageUrl,
            LinkUrl = body.LinkUrl,
            ActionType = body.ActionType,
            Placement = Enum.TryParse<AdPlacement>(body.Placement, true, out var p) ? p : AdPlacement.Banner,
            StartAtUtc = body.StartAtUtc,
            EndAtUtc = body.EndAtUtc,
            IsActive = true,
            DisplayPriority = body.DisplayPriority
        };
        _db.Advertisements.Add(ad);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { ad.Id, ad.Title }));
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<object>>> Users(CancellationToken ct)
    {
        var list = await _db.Users.AsNoTracking()
            .Where(u => !u.IsDeleted)
            .Select(u => new { u.Id, u.Name, u.Email, u.Role, u.Balance, u.IsActive, u.CreatedAtUtc })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(list));
    }

    [HttpPatch("users/{id:guid}/balance")]
    public async Task<ActionResult<ApiResponse<object>>> AdjustBalance(Guid id, [FromBody] BalanceBody body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Reason))
            return BadRequest(ApiResponse<object>.Fail("Reason is required for balance adjustments."));
        var balance = await _ledger.AdjustBalanceAsync(id, body.Amount, body.Reason!, AdminId, ct);
        return Ok(ApiResponse<object>.Ok(new { id, balance }));
    }

    public record StatusBody(string Status);
    public record FabricBody(string Name, string? Description, string? ImageUrl, decimal PriceAdjustment);
    public record CutBody(string Name, string? Description, string? ImageUrl, decimal PriceAdjustment);
    public record SizeBody(string Code, string Name, decimal Width, decimal Height, decimal? Depth, string? Unit, decimal PriceAdjustment);
    public record EmbroideryBody(decimal PricePerSquareUnit, decimal? MinimumCharge);
    public record PrintingBody(string Name, string Code, string? Description, decimal Price, List<string>? IncludedSurfaceCodes);
    public record AssetBody(string Name, string FileUrl, string? ThumbnailUrl, Guid? CategoryId, List<string>? Tags, List<Guid>? CompatibleProductIds, List<string>? CompatibleSurfaceCodes);
    public record FeatureBody(bool IsFeatured, int Priority);
    public record AdBody(string Title, string? Description, string? ImageUrl, string? LinkUrl, string? ActionType, string? Placement, DateTime? StartAtUtc, DateTime? EndAtUtc, int DisplayPriority);
    public record AdUpdateBody(string? Title, string? Description, string? ImageUrl, string? LinkUrl, bool? IsActive, int? DisplayPriority, DateTime? StartAtUtc, DateTime? EndAtUtc);
    public record BalanceBody(decimal Amount, string? Reason);
}
