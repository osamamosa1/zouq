using Microsoft.EntityFrameworkCore;
using Zouq.Application.DTOs;
using Zouq.Application.Interfaces;
using Zouq.Domain.Designs;
using Zouq.Domain.Entities;
using Zouq.Domain.Enums;
using Zouq.Domain.Orders;
using Zouq.Infrastructure.Data;

namespace Zouq.Infrastructure.Services;

public class AuthAppService
{
    private readonly ZouqDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public AuthAppService(ZouqDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _db = db; _hasher = hasher; _jwt = jwt;
    }

    public async Task<AuthTokenResponse> RegisterAsync(AuthRegisterRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            Name = req.Name.Trim(),
            Email = email,
            Phone = req.Phone,
            PasswordHash = _hasher.Hash(req.Password),
            Role = UserRole.Customer
        };
        _db.Users.Add(user);
        var tokens = await IssueTokensAsync(user, ct);
        await _db.SaveChangesAsync(ct);
        return tokens;
    }

    public async Task<AuthTokenResponse> LoginAsync(AuthLoginRequest req, CancellationToken ct = default)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct)
            ?? throw new InvalidOperationException("Invalid credentials.");
        if (!user.IsActive || !_hasher.Verify(req.Password, user.PasswordHash))
            throw new InvalidOperationException("Invalid credentials.");
        var tokens = await IssueTokensAsync(user, ct);
        await _db.SaveChangesAsync(ct);
        return tokens;
    }

    public async Task<AuthTokenResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await _db.RefreshTokens.Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken && !t.IsRevoked, ct)
            ?? throw new InvalidOperationException("Invalid refresh token.");
        if (stored.ExpiresAtUtc < DateTime.UtcNow)
            throw new InvalidOperationException("Refresh token expired.");
        stored.IsRevoked = true;
        var tokens = await IssueTokensAsync(stored.User, ct);
        await _db.SaveChangesAsync(ct);
        return tokens;
    }

    private async Task<AuthTokenResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var access = _jwt.GenerateAccessToken(user);
        var refresh = _jwt.GenerateRefreshToken();
        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refresh,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(30)
        });
        await Task.CompletedTask;
        return new AuthTokenResponse(access, refresh, _jwt.AccessTokenExpiresSeconds, MapUser(user));
    }

    public static UserDto MapUser(User u) =>
        new(u.Id, u.Name, u.Email, u.Phone, u.Role.ToString(), u.AvatarUrl, u.Balance);
}

public class CatalogAppService
{
    private readonly ZouqDbContext _db;

    public CatalogAppService(ZouqDbContext db) => _db = db;

    public async Task<IReadOnlyList<ProductListItemDto>> ListProductsAsync(CancellationToken ct = default)
    {
        return await _db.Products.AsNoTracking()
            .Include(p => p.ProductType)
            .Where(p => !p.IsDeleted && p.Status == EntityStatus.Active)
            .OrderBy(p => p.SortOrder).ThenBy(p => p.Name)
            .Select(p => new ProductListItemDto(
                p.Id, p.Name, p.Slug, p.ThumbnailUrl, p.BasePrice,
                p.ProductType.Name, p.MeasurementUnit.ToString()))
            .ToListAsync(ct);
    }

    public async Task<ProductConfigDto> GetProductConfigAsync(Guid productId, CancellationToken ct = default)
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

        return new ProductConfigDto(
            p.Id, p.Name, p.Slug, p.Description, p.ThumbnailUrl, p.BasePrice,
            p.MeasurementUnit.ToString(), p.ProductType.Name,
            p.Surfaces.Where(s => !s.IsDeleted && s.Status == EntityStatus.Active).OrderBy(s => s.SortOrder)
                .Select(s => new SurfaceDto(s.Id, s.Code, s.Name, s.PreviewImageUrl, s.IsRequired,
                    s.DesignArea == null ? null : new DesignAreaDto(
                        s.DesignArea.NormX, s.DesignArea.NormY, s.DesignArea.NormWidth, s.DesignArea.NormHeight,
                        s.DesignArea.RealWidth, s.DesignArea.RealHeight))).ToList(),
            p.ProductFabrics.Where(f => !f.IsDeleted && f.Fabric.Status == EntityStatus.Active)
                .Select(f => new FabricDto(f.Fabric.Id, f.Fabric.Name, f.Fabric.Description, f.Fabric.ImageUrl, f.Fabric.PriceAdjustment)).ToList(),
            p.ProductCuts.Where(c => !c.IsDeleted && c.CutStyle.Status == EntityStatus.Active)
                .Select(c => new CutStyleDto(c.CutStyle.Id, c.CutStyle.Name, c.CutStyle.Description, c.CutStyle.ImageUrl, c.CutStyle.PriceAdjustment)).ToList(),
            p.Sizes.Where(s => !s.IsDeleted && s.Status == EntityStatus.Active).OrderBy(s => s.SortOrder)
                .Select(s => new SizeDto(s.Id, s.Code, s.Name, s.Width, s.Height, s.Depth, s.Unit.ToString(), s.PriceAdjustment)).ToList(),
            p.PrintingOptions.Where(o => !o.IsDeleted && o.Status == EntityStatus.Active).OrderBy(o => o.SortOrder)
                .Select(o => new PrintingOptionDto(o.Id, o.Name, o.Code, o.Description, o.Price, o.IncludedSurfaceCodes)).ToList(),
            p.EmbroideryPricing == null || p.EmbroideryPricing.Status != EntityStatus.Active
                ? null
                : new EmbroideryPricingDto(p.EmbroideryPricing.PricePerSquareUnit, p.EmbroideryPricing.Unit.ToString(), p.EmbroideryPricing.MinimumCharge));
    }

    public async Task<IReadOnlyList<DesignAssetDto>> ListAssetsAsync(Guid? productId, string? surfaceCode, CancellationToken ct = default)
    {
        var q = _db.DesignAssets.AsNoTracking().Include(a => a.Category)
            .Where(a => !a.IsDeleted && a.Status == EntityStatus.Active);

        var list = await q.OrderByDescending(a => a.IsFeatured).ThenBy(a => a.SortOrder).ThenBy(a => a.Name).ToListAsync(ct);
        if (productId is Guid pid)
            list = list.Where(a => a.CompatibleProductIds.Count == 0 || a.CompatibleProductIds.Contains(pid)).ToList();
        if (!string.IsNullOrWhiteSpace(surfaceCode))
            list = list.Where(a => a.CompatibleSurfaceCodes.Count == 0 ||
                a.CompatibleSurfaceCodes.Any(s => s.Equals(surfaceCode, StringComparison.OrdinalIgnoreCase))).ToList();

        return list.Select(a => new DesignAssetDto(a.Id, a.Name, a.FileUrl, a.ThumbnailUrl, a.Category?.Name, a.Tags)).ToList();
    }
}

public class DesignAppService
{
    private readonly ZouqDbContext _db;
    private readonly IPriceCalculationService _pricing;
    private readonly DesignIntegrityService _integrity;

    public DesignAppService(ZouqDbContext db, IPriceCalculationService pricing, DesignIntegrityService integrity)
    {
        _db = db; _pricing = pricing; _integrity = integrity;
    }

    public async Task<DesignDto> SaveAsync(Guid userId, SaveDesignRequest req, Guid? designId = null, CancellationToken ct = default)
    {
        Design design;
        if (designId is Guid id)
        {
            design = await _db.Designs.Include(d => d.Elements)
                .FirstOrDefaultAsync(d => d.Id == id && d.OwnerId == userId && !d.IsDeleted, ct)
                ?? throw new InvalidOperationException("Design not found.");

            if (DesignLifecycleRules.IsImmutable(design.Status))
                throw new InvalidOperationException("Design used in an order is immutable.");

            _db.DesignElements.RemoveRange(design.Elements);
            // Preserve DerivedFromDesignId — attribution must survive edits
        }
        else
        {
            design = new Design { OwnerId = userId };
            _db.Designs.Add(design);
        }

        // Saves are always drafts — never auto-publish to For You
        design.ProductId = req.ProductId;
        design.FabricId = req.FabricId;
        design.CutStyleId = req.CutStyleId;
        design.ProductSizeId = req.ProductSizeId;
        design.PrintingOptionId = req.PrintingOptionId;
        design.Title = req.Title;
        design.Description = req.Description;
        design.Status = DesignStatus.Draft;
        design.Visibility = DesignVisibility.Private;
        if (Enum.TryParse<DesignVisibility>(req.Visibility, true, out var vis) && vis != DesignVisibility.Public)
            design.Visibility = vis;

        foreach (var el in req.Elements)
        {
            if (!Enum.TryParse<ProductionMethodCode>(el.ProductionMethod, true, out var method))
                throw new InvalidOperationException($"Unknown production method: {el.ProductionMethod}");

            design.Elements.Add(new DesignElement
            {
                SurfaceCode = el.SurfaceCode,
                DesignAssetId = el.DesignAssetId,
                UserUploadId = el.UserUploadId,
                ProductionMethod = method,
                NormX = el.NormX,
                NormY = el.NormY,
                NormWidth = el.NormWidth,
                NormHeight = el.NormHeight,
                RotationDegrees = el.RotationDegrees,
                Scale = el.Scale,
                // Placeholder — overwritten by integrity service
                RealWidth = 0,
                RealHeight = 0,
                ZIndex = el.ZIndex
            });
        }

        await _integrity.ValidateAndNormalizeDesignAsync(design, ct);

        var quote = await _pricing.CalculateDesignPriceAsync(new PriceCalculationRequest(
            design.ProductId, design.FabricId, design.CutStyleId, design.ProductSizeId, design.PrintingOptionId,
            design.Elements.Select(e => new ElementPriceInput(e.SurfaceCode, e.ProductionMethod, e.RealWidth, e.RealHeight)).ToList()), ct);
        design.LastEstimatedPrice = quote.Total;

        await _db.SaveChangesAsync(ct);
        return await GetAsync(design.Id, userId, ct);
    }

    public async Task<DesignDto> GetAsync(Guid designId, Guid? requesterId, CancellationToken ct = default)
    {
        var d = await _db.Designs.AsNoTracking().Include(x => x.Elements)
            .FirstOrDefaultAsync(x => x.Id == designId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Design not found.");

        if (d.Visibility == DesignVisibility.Private && d.OwnerId != requesterId)
            throw new UnauthorizedAccessException("Design is private.");

        return Map(d);
    }

    public async Task<IReadOnlyList<DesignDto>> MyDesignsAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await _db.Designs.AsNoTracking().Include(d => d.Elements)
            .Where(d => d.OwnerId == userId && !d.IsDeleted)
            .OrderByDescending(d => d.UpdatedAtUtc)
            .ToListAsync(ct);
        return list.Select(Map).ToList();
    }

    /// <summary>
    /// Explicit publish to For You / reuse. Requires DeliveredEligible (order was Delivered).
    /// </summary>
    public async Task PublishAsync(Guid userId, Guid designId, CancellationToken ct = default)
    {
        var d = await _db.Designs.FirstOrDefaultAsync(x => x.Id == designId && x.OwnerId == userId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Design not found.");
        DesignLifecycleRules.EnsureCanPublishAsReusable(d.Status);
        d.Status = DesignStatus.Reusable;
        d.Visibility = DesignVisibility.Public;
        d.PublishedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task UnpublishAsync(Guid userId, Guid designId, CancellationToken ct = default)
    {
        var d = await _db.Designs.FirstOrDefaultAsync(x => x.Id == designId && x.OwnerId == userId && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Design not found.");
        if (d.Status != DesignStatus.Reusable)
            throw new InvalidOperationException("Design is not published.");
        d.Status = DesignStatus.DeliveredEligible;
        d.Visibility = DesignVisibility.Private;
        d.IsFeatured = false;
        d.PublishedAtUtc = null;
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Clone a published reusable design into a new private draft owned by <paramref name="userId"/>.
    /// Never mutates the source design.
    /// </summary>
    public async Task<DesignDto> DeriveAsync(Guid userId, Guid sourceDesignId, CancellationToken ct = default)
    {
        var source = await _db.Designs.AsNoTracking()
            .Include(d => d.Elements)
            .Include(d => d.SourceOrder)
            .FirstOrDefaultAsync(d => d.Id == sourceDesignId && !d.IsDeleted, ct)
            ?? throw new InvalidOperationException("Design not found.");

        DesignLifecycleRules.EnsureCanDeriveAsReusable(source.Status, source.Visibility, source.IsDeleted);

        // Extra gate: related order must still be Delivered (Reusable implies this; refund revokes Reusable)
        if (source.SourceOrderId is Guid oid)
        {
            var orderStatus = source.SourceOrder?.Status
                ?? await _db.Orders.AsNoTracking().Where(o => o.Id == oid).Select(o => o.Status).FirstAsync(ct);
            if (OrderStatusRules.Normalize(orderStatus) != OrderStatus.Delivered)
                throw new UnauthorizedAccessException("Source design is not eligible for reuse.");
        }

        var draft = new Design
        {
            OwnerId = userId,
            ProductId = source.ProductId,
            FabricId = source.FabricId,
            CutStyleId = source.CutStyleId,
            ProductSizeId = source.ProductSizeId,
            PrintingOptionId = source.PrintingOptionId,
            Title = source.Title,
            Description = source.Description,
            PreviewImageUrl = source.PreviewImageUrl,
            Status = DesignStatus.Draft,
            Visibility = DesignVisibility.Private,
            Tags = source.Tags.ToList(),
            LastEstimatedPrice = source.LastEstimatedPrice,
            DerivedFromDesignId = source.Id,
            // Do not copy SourceOrderId / publish flags — this is a new draft
        };

        foreach (var el in source.Elements.Where(e => !e.IsDeleted))
        {
            draft.Elements.Add(new DesignElement
            {
                SurfaceCode = el.SurfaceCode,
                DesignAssetId = el.DesignAssetId,
                UserUploadId = el.UserUploadId,
                ProductionMethod = el.ProductionMethod,
                NormX = el.NormX,
                NormY = el.NormY,
                NormWidth = el.NormWidth,
                NormHeight = el.NormHeight,
                RotationDegrees = el.RotationDegrees,
                Scale = el.Scale,
                RealWidth = el.RealWidth,
                RealHeight = el.RealHeight,
                ZIndex = el.ZIndex,
                Metadata = new Dictionary<string, string>(el.Metadata)
            });
        }

        _db.Designs.Add(draft);
        await _db.SaveChangesAsync(ct);
        return await GetAsync(draft.Id, userId, ct);
    }

    private static DesignDto Map(Design d) => new(
        d.Id, d.Title, d.Description, d.Status.ToString(), d.Visibility.ToString(), d.IsFeatured,
        d.PreviewImageUrl, d.ProductId, d.FabricId, d.CutStyleId, d.ProductSizeId, d.PrintingOptionId,
        d.LastEstimatedPrice, d.Tags,
        d.Elements.Select(e => new DesignElementDto(
            e.Id, e.SurfaceCode, e.DesignAssetId, e.UserUploadId, e.ProductionMethod.ToString(),
            e.NormX, e.NormY, e.NormWidth, e.NormHeight, e.RotationDegrees, e.Scale,
            e.RealWidth, e.RealHeight, e.ZIndex)).ToList(),
        d.CreatedAtUtc, d.UpdatedAtUtc,
        d.DerivedFromDesignId);
}

public class FeedAppService
{
    private readonly ZouqDbContext _db;

    public FeedAppService(ZouqDbContext db) => _db = db;

    public async Task<IReadOnlyList<FeedItemDto>> ForYouAsync(Guid? userId, int take = 40, int skip = 0, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(0, skip);

        var interestTags = userId is Guid uid
            ? await _db.UserInterestTags.AsNoTracking().Where(t => t.UserId == uid).Select(t => t.Tag).ToListAsync(ct)
            : new List<string>();

        // ONLY Delivered-eligible designs that were explicitly published as Reusable
        var query = _db.Designs.AsNoTracking()
            .Include(d => d.Owner)
            .Where(d => !d.IsDeleted
                        && d.Status == DesignStatus.Reusable
                        && d.Visibility == DesignVisibility.Public);

        var designs = await query
            .OrderByDescending(d => d.IsFeatured)
            .ThenByDescending(d => d.FeaturedPriority)
            .ThenByDescending(d => d.PublishedAtUtc ?? d.UpdatedAtUtc)
            .Skip(skip)
            .Take(Math.Min(take * 3, 200)) // over-fetch slightly for interest re-rank in memory window
            .ToListAsync(ct);

        var ranked = designs
            .Select(d =>
            {
                var overlap = interestTags.Count == 0 ? 0 :
                    d.Tags.Count(t => interestTags.Any(i => i.Equals(t, StringComparison.OrdinalIgnoreCase)));
                var score = (d.IsFeatured ? 1_000_000 : 0) + (d.FeaturedPriority * 10_000) + (overlap * 100);
                return (d, score);
            })
            .OrderByDescending(x => x.score)
            .ThenByDescending(x => x.d.PublishedAtUtc ?? x.d.UpdatedAtUtc)
            .Take(take)
            .Select(x => new FeedItemDto(
                x.d.Id, x.d.Title, x.d.PreviewImageUrl, x.d.Owner.Name,
                x.d.IsFeatured, x.d.FeaturedPriority, x.d.Tags, x.d.LastEstimatedPrice))
            .ToList();

        return ranked;
    }

    public async Task<IReadOnlyList<AdDto>> ActiveAdsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _db.Advertisements.AsNoTracking()
            .Where(a => a.IsActive && !a.IsDeleted
                && (a.StartAtUtc == null || a.StartAtUtc <= now)
                && (a.EndAtUtc == null || a.EndAtUtc >= now))
            .OrderByDescending(a => a.DisplayPriority)
            .Select(a => new AdDto(a.Id, a.Title, a.Description, a.ImageUrl, a.LinkUrl, a.ActionType, a.Placement.ToString(), a.DisplayPriority))
            .ToListAsync(ct);
    }
}
