using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zouq.Application.Interfaces;
using Zouq.Domain.Designs;
using Zouq.Domain.Entities;
using Zouq.Domain.Enums;
using Zouq.Domain.Orders;
using Zouq.Infrastructure.Data;

namespace Zouq.Infrastructure.Services;

/// <summary>
/// Order status machine + creator reward lifecycle (Delivered credit / Refunded reversal).
/// Order create validates catalog membership, design-area bounds, and recomputes real sizes.
/// Published reusable designs are never mutated when reused — only owned drafts become Ordered.
/// </summary>
public class OrderAppService
{
    private readonly ZouqDbContext _db;
    private readonly IPriceCalculationService _pricing;
    private readonly FinancialLedgerService _ledger;
    private readonly DesignIntegrityService _integrity;
    private readonly ILogger<OrderAppService> _logger;
    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public OrderAppService(
        ZouqDbContext db,
        IPriceCalculationService pricing,
        FinancialLedgerService ledger,
        DesignIntegrityService integrity,
        ILogger<OrderAppService>? logger = null)
    {
        _db = db;
        _pricing = pricing;
        _ledger = ledger;
        _integrity = integrity;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<OrderAppService>.Instance;
    }

    public async Task<Application.DTOs.OrderDto> CreateFromDesignAsync(
        Guid buyerId,
        Application.DTOs.CreateOrderRequest req,
        CancellationToken ct = default)
    {
        if (req.Quantity < 1) throw new InvalidOperationException("Quantity must be at least 1.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var design = await _db.Designs
            .Include(d => d.Elements)
            .Include(d => d.Product)
            .Include(d => d.Owner)
            .Include(d => d.DerivedFromDesign)
                .ThenInclude(s => s!.Owner)
            .FirstOrDefaultAsync(d => d.Id == req.DesignId && !d.IsDeleted, ct)
            ?? throw new InvalidOperationException("Design not found.");

        // Authorization: own editable draft only.
        // Reuse of published designs must go through DeriveAsync first (new draft owned by buyer).
        if (DesignLifecycleRules.IsPublicReusable(design.Status, design.Visibility) && design.OwnerId != buyerId)
            throw new UnauthorizedAccessException(
                "Cannot order a published design directly. Derive it first (Use Design) to create your own draft.");

        DesignLifecycleRules.EnsureCanOrderAsOwnedDraft(buyerId, design.OwnerId, design.Status);

        var product = await _db.Products
            .Include(p => p.ProductType)
            .Include(p => p.Surfaces).ThenInclude(s => s.DesignArea)
            .Include(p => p.ProductFabrics).ThenInclude(f => f.Fabric)
            .Include(p => p.ProductCuts).ThenInclude(c => c.CutStyle)
            .Include(p => p.Sizes)
            .Include(p => p.PrintingOptions)
            .Include(p => p.EmbroideryPricing)
            .FirstAsync(p => p.Id == design.ProductId, ct);

        await _integrity.ValidateCatalogOptionsAsync(
            product, design.FabricId, design.CutStyleId, design.ProductSizeId, design.PrintingOptionId, design.Elements, ct);

        _integrity.NormalizeElementGeometry(product, design.Elements);

        var breakdown = await _pricing.CalculateDesignPriceAsync(new PriceCalculationRequest(
            design.ProductId, design.FabricId, design.CutStyleId, design.ProductSizeId, design.PrintingOptionId,
            design.Elements.Select(e => new ElementPriceInput(
                e.SurfaceCode, e.ProductionMethod, e.RealWidth, e.RealHeight)).ToList()), ct);

        var unit = breakdown.Total;
        var lineTotal = unit * req.Quantity;

        var commission = await _db.CommissionSettings.AsNoTracking()
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderByDescending(c => c.UpdatedAtUtc)
            .FirstOrDefaultAsync(ct);
        var commissionPercent = commission?.DesignerCommissionPercent ?? 0m;

        var fabric = design.FabricId is Guid fid
            ? product.ProductFabrics.First(f => f.FabricId == fid).Fabric
            : null;
        var cut = design.CutStyleId is Guid cid
            ? product.ProductCuts.First(c => c.CutStyleId == cid).CutStyle
            : null;
        var size = design.ProductSizeId is Guid sid
            ? product.Sizes.First(s => s.Id == sid)
            : null;
        var print = design.PrintingOptionId is Guid pid
            ? product.PrintingOptions.First(o => o.Id == pid)
            : null;

        // Creator attribution: derived drafts credit the published source owner
        var (creatorId, creatorName, creatorEmail, publishedSourceId) = await ResolveCreatorAsync(design, ct);

        var order = new Order
        {
            OrderNumber = $"ZQ-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}",
            BuyerId = buyerId,
            Status = OrderStatus.Pending,
            Subtotal = lineTotal,
            Total = lineTotal,
            CommissionPercentSnapshot = commissionPercent,
            CreatorRewardAmount = null,
            ShippingAddressJson = req.ShippingAddressJson,
            Notes = req.Notes,
            Items =
            {
                new OrderItem
                {
                    // Points at the buyer's ordered draft (never the published source row)
                    SourceDesignId = design.Id,
                    DesignerId = creatorId,
                    Quantity = req.Quantity,
                    UnitPrice = unit,
                    LineTotal = lineTotal,
                    DesignSnapshotJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        design.Id,
                        design.Title,
                        design.ProductId,
                        design.FabricId,
                        design.CutStyleId,
                        design.ProductSizeId,
                        design.PrintingOptionId,
                        DerivedFromDesignId = design.DerivedFromDesignId,
                        PublishedSourceDesignId = publishedSourceId,
                        Creator = new { Id = creatorId, Name = creatorName, Email = creatorEmail },
                        DraftOwnerId = design.OwnerId,
                        CommissionPercentSnapshot = commissionPercent,
                        Surfaces = design.Elements.Select(e => e.SurfaceCode).Distinct().ToList(),
                        Elements = design.Elements.Select(e => new
                        {
                            e.SurfaceCode, e.DesignAssetId, e.UserUploadId,
                            ProductionMethod = e.ProductionMethod.ToString(),
                            e.NormX, e.NormY, e.NormWidth, e.NormHeight,
                            e.RotationDegrees, e.Scale,
                            e.RealWidth, e.RealHeight, e.ZIndex
                        })
                    }, JsonOpts),
                    PricingBreakdownJson = System.Text.Json.JsonSerializer.Serialize(breakdown, JsonOpts),
                    ProductSnapshotJson = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        product.Id,
                        product.Name,
                        ProductType = product.ProductType?.Name,
                        BasePrice = product.BasePrice,
                        Unit = product.MeasurementUnit.ToString(),
                        Fabric = fabric == null ? null : new { fabric.Id, fabric.Name, fabric.PriceAdjustment },
                        Cut = cut == null ? null : new { cut.Id, cut.Name, cut.PriceAdjustment },
                        Size = size == null ? null : new { size.Id, size.Code, size.Name, size.Width, size.Height, size.Depth, size.PriceAdjustment },
                        Printing = print == null ? null : new { print.Id, print.Name, print.Code, print.Price, print.IncludedSurfaceCodes },
                        Embroidery = product.EmbroideryPricing == null ? null : new
                        {
                            product.EmbroideryPricing.PricePerSquareUnit,
                            Unit = product.EmbroideryPricing.Unit.ToString(),
                            product.EmbroideryPricing.MinimumCharge
                        },
                        Creator = new { Id = creatorId, Name = creatorName }
                    }, JsonOpts)
                }
            }
        };

        // Mutate ONLY the buyer's draft — never a published reusable source
        design.Status = DesignStatus.Ordered;
        design.Visibility = DesignVisibility.Private;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        design.SourceOrderId = order.Id;
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        _logger.LogInformation(
            "Order created: {OrderNumber} Buyer={BuyerId} Design={DesignId} Creator={CreatorId} DerivedFrom={DerivedFrom} Total={Total} CommissionSnapshot={Commission}% (no creator credit)",
            order.OrderNumber, buyerId, design.Id, creatorId, design.DerivedFromDesignId, order.Total, commissionPercent);
        return Map(order);
    }

    /// <summary>
    /// Creator for rewards = published source owner when derived; otherwise draft owner.
    /// </summary>
    private async Task<(Guid CreatorId, string Name, string Email, Guid? PublishedSourceId)> ResolveCreatorAsync(
        Design design, CancellationToken ct)
    {
        if (design.DerivedFromDesignId is Guid sourceId)
        {
            var source = design.DerivedFromDesign
                ?? await _db.Designs.AsNoTracking().Include(d => d.Owner)
                    .FirstAsync(d => d.Id == sourceId, ct);
            return (source.OwnerId, source.Owner.Name, source.Owner.Email, source.Id);
        }

        return (design.OwnerId, design.Owner.Name, design.Owner.Email, null);
    }

    public async Task<Application.DTOs.OrderDto> TransitionStatusAsync(
        Guid orderId,
        OrderStatus targetStatus,
        Guid? adminUserId = null,
        CancellationToken ct = default)
    {
        targetStatus = OrderStatusRules.Normalize(targetStatus);

        const int maxAttempts = 5;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await TransitionStatusOnceAsync(orderId, targetStatus, ct);
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxAttempts)
            {
                foreach (var entry in _db.ChangeTracker.Entries().ToList())
                    entry.State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException("Could not update order finances due to concurrent balance updates.");
    }

    private async Task<Application.DTOs.OrderDto> TransitionStatusOnceAsync(
        Guid orderId,
        OrderStatus targetStatus,
        CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.CreatorRewards)
            .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted, ct)
            ?? throw new InvalidOperationException("Order not found.");

        var from = OrderStatusRules.Normalize(order.Status);
        targetStatus = OrderStatusRules.Normalize(targetStatus);

        if (from == targetStatus)
        {
            await tx.CommitAsync(ct);
            return Map(order);
        }

        OrderStatusRules.EnsureCanTransition(from, targetStatus);
        order.Status = targetStatus;
        _logger.LogInformation("Order status transition: {OrderId} {From} → {To}", orderId, from, targetStatus);

        if (targetStatus == OrderStatus.Delivered)
        {
            await _ledger.CreditCreatorRewardsForDeliveredOrderAsync(order, ct);
            await MarkDesignsDeliveredEligibleAsync(order, ct);
        }
        else if (targetStatus == OrderStatus.Refunded)
        {
            await _ledger.ReverseCreatorRewardsForOrderAsync(order, "Order refunded — creator reward reversal", ct);
            await RevokeDesignEligibilityOnRefundAsync(order, ct);
        }
        else if (targetStatus == OrderStatus.Cancelled)
        {
            if (order.CreatorRewards.Any(r => r.Status == CreatorRewardStatus.Credited && !r.IsDeleted))
                throw new InvalidOperationException("Cannot cancel an order with an active creator reward.");
        }

        try
        {
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await tx.RollbackAsync(ct);
            foreach (var entry in _db.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;

            var reloaded = await _db.Orders.AsNoTracking().Include(o => o.Items)
                .FirstAsync(o => o.Id == orderId, ct);
            if (OrderStatusRules.Normalize(reloaded.Status) == OrderStatus.Delivered && targetStatus == OrderStatus.Delivered)
                return Map(reloaded);
            throw;
        }

        return Map(order);
    }

    private async Task MarkDesignsDeliveredEligibleAsync(Order order, CancellationToken ct)
    {
        var designIds = order.Items
            .Where(i => i.SourceDesignId.HasValue)
            .Select(i => i.SourceDesignId!.Value)
            .Distinct()
            .ToList();

        if (designIds.Count == 0) return;

        var designs = await _db.Designs
            .Where(d => designIds.Contains(d.Id) && !d.IsDeleted)
            .ToListAsync(ct);

        foreach (var d in designs)
        {
            // Only Ordered buyer drafts become eligible — never touch published Reusable sources
            if (d.Status is DesignStatus.Ordered or DesignStatus.UsedInOrder)
            {
                d.Status = DesignStatus.DeliveredEligible;
                d.BecameEligibleAtUtc = DateTime.UtcNow;
                d.SourceOrderId ??= order.Id;
                if (d.Visibility == DesignVisibility.Public)
                    d.Visibility = DesignVisibility.Private;
            }
        }
    }

    private async Task RevokeDesignEligibilityOnRefundAsync(Order order, CancellationToken ct)
    {
        var designIds = order.Items
            .Where(i => i.SourceDesignId.HasValue)
            .Select(i => i.SourceDesignId!.Value)
            .Distinct()
            .ToList();
        if (designIds.Count == 0) return;

        var designs = await _db.Designs.Where(d => designIds.Contains(d.Id)).ToListAsync(ct);
        foreach (var d in designs)
        {
            // Order items point at the buyer's ordered draft only — never at an untouched published source.
            // Refunding the original publish order may revoke that design's Reusable status.
            if (d.Status is DesignStatus.DeliveredEligible or DesignStatus.Reusable)
            {
                d.Status = DesignStatus.Ordered;
                d.Visibility = DesignVisibility.Private;
                d.IsFeatured = false;
                d.PublishedAtUtc = null;
                d.BecameEligibleAtUtc = null;
            }
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var msg = (ex.InnerException?.Message ?? ex.Message);
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("unique constraint", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("2627")
               || msg.Contains("2601")
               || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<Application.DTOs.OrderDto>> MyOrdersAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await _db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.BuyerId == userId && !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<Application.DTOs.OrderDetailDto> GetMineAsync(Guid userId, Guid orderId, CancellationToken ct = default)
    {
        var o = await _db.Orders.AsNoTracking().Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId && x.BuyerId == userId && !x.IsDeleted, ct)
            ?? throw new UnauthorizedAccessException("Order not found or not owned by you.");

        return new Application.DTOs.OrderDetailDto(
            o.Id, o.OrderNumber, OrderStatusRules.Normalize(o.Status).ToString(),
            o.Subtotal, o.Total, o.Currency, o.CreatedAtUtc,
            o.CommissionPercentSnapshot, o.CreatorRewardAmount,
            o.Items.Select(i => new Application.DTOs.OrderDetailItemDto(
                i.Id, i.SourceDesignId, i.DesignerId, i.Quantity, i.UnitPrice, i.LineTotal,
                i.DesignSnapshotJson, i.PricingBreakdownJson, i.ProductSnapshotJson)).ToList());
    }

    private static Application.DTOs.OrderDto Map(Order o) => new(
        o.Id, o.OrderNumber, OrderStatusRules.Normalize(o.Status).ToString(), o.Subtotal, o.Total, o.Currency, o.CreatedAtUtc,
        o.Items.Select(i => new Application.DTOs.OrderItemDto(i.Id, i.SourceDesignId, i.Quantity, i.UnitPrice, i.LineTotal)).ToList());
}
