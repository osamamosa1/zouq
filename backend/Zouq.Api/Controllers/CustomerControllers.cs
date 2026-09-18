using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Zouq.Application.DTOs;
using Zouq.Application.Interfaces;
using Zouq.Domain.Common;
using Zouq.Domain.Entities;
using Zouq.Domain.Enums;
using Zouq.Infrastructure.Data;
using Zouq.Infrastructure.Services;

namespace Zouq.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthAppService _auth;
    public AuthController(AuthAppService auth) => _auth = auth;

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Register([FromBody] AuthRegisterRequest req, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(req, ct);
        return Ok(ApiResponse<AuthTokenResponse>.Ok(result));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Login([FromBody] AuthLoginRequest req, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req, ct);
        return Ok(ApiResponse<AuthTokenResponse>.Ok(result));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthTokenResponse>>> Refresh([FromBody] RefreshTokenRequest req, CancellationToken ct)
    {
        var result = await _auth.RefreshAsync(req.RefreshToken, ct);
        return Ok(ApiResponse<AuthTokenResponse>.Ok(result));
    }
}

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly CatalogAppService _catalog;
    public ProductsController(CatalogAppService catalog) => _catalog = catalog;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProductListItemDto>>>> List(CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<ProductListItemDto>>.Ok(await _catalog.ListProductsAsync(ct)));

    [HttpGet("{id:guid}/config")]
    public async Task<ActionResult<ApiResponse<ProductConfigDto>>> Config(Guid id, CancellationToken ct)
        => Ok(ApiResponse<ProductConfigDto>.Ok(await _catalog.GetProductConfigAsync(id, ct)));
}

[ApiController]
[Route("api/assets")]
[EnableRateLimiting("feed")]
public class AssetsController : ControllerBase
{
    private readonly CatalogAppService _catalog;
    public AssetsController(CatalogAppService catalog) => _catalog = catalog;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DesignAssetDto>>>> List(
        [FromQuery] Guid? productId, [FromQuery] string? surface, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<DesignAssetDto>>.Ok(await _catalog.ListAssetsAsync(productId, surface, ct)));
}

[ApiController]
[Authorize]
[Route("api/designs")]
[EnableRateLimiting("mutate")]
public class DesignsController : ControllerBase
{
    private readonly DesignAppService _designs;
    private readonly IPriceCalculationService _pricing;
    private readonly DesignIntegrityService _integrity;
    private readonly ZouqDbContext _db;

    public DesignsController(
        DesignAppService designs,
        IPriceCalculationService pricing,
        DesignIntegrityService integrity,
        ZouqDbContext db)
    {
        _designs = designs;
        _pricing = pricing;
        _integrity = integrity;
        _db = db;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DesignDto>>>> Mine(CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<DesignDto>>.Ok(await _designs.MyDesignsAsync(UserId, ct)));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<DesignDto>>> Get(Guid id, CancellationToken ct)
    {
        Guid? requester = User.Identity?.IsAuthenticated == true ? UserId : null;
        return Ok(ApiResponse<DesignDto>.Ok(await _designs.GetAsync(id, requester, ct)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DesignDto>>> Create([FromBody] SaveDesignRequest req, CancellationToken ct)
        => Ok(ApiResponse<DesignDto>.Ok(await _designs.SaveAsync(UserId, req, null, ct)));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DesignDto>>> Update(Guid id, [FromBody] SaveDesignRequest req, CancellationToken ct)
        => Ok(ApiResponse<DesignDto>.Ok(await _designs.SaveAsync(UserId, req, id, ct)));

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ApiResponse<object>>> Publish(Guid id, CancellationToken ct)
    {
        await _designs.PublishAsync(UserId, id, ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Published for reuse"));
    }

    [HttpPost("{id:guid}/unpublish")]
    public async Task<ActionResult<ApiResponse<object>>> Unpublish(Guid id, CancellationToken ct)
    {
        await _designs.UnpublishAsync(UserId, id, ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Unpublished"));
    }

    /// <summary>
    /// Clone a published reusable design into a new private draft for the current user.
    /// Source design is never mutated.
    /// </summary>
    [HttpPost("{id:guid}/derive")]
    public async Task<ActionResult<ApiResponse<DesignDto>>> Derive(Guid id, CancellationToken ct)
        => Ok(ApiResponse<DesignDto>.Ok(await _designs.DeriveAsync(UserId, id, ct)));

    [HttpPost("quote")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PriceBreakdown>>> Quote([FromBody] QuotePriceRequest req, CancellationToken ct)
    {
        var product = await _db.Products
            .Include(p => p.Surfaces).ThenInclude(s => s.DesignArea)
            .Include(p => p.ProductFabrics).ThenInclude(f => f.Fabric)
            .Include(p => p.ProductCuts).ThenInclude(c => c.CutStyle)
            .Include(p => p.Sizes)
            .Include(p => p.PrintingOptions)
            .FirstOrDefaultAsync(p => p.Id == req.ProductId && !p.IsDeleted, ct)
            ?? throw new InvalidOperationException("Product not found.");

        var elements = new List<DesignElement>();
        foreach (var e in req.Elements)
        {
            if (!Enum.TryParse<ProductionMethodCode>(e.ProductionMethod, true, out var m))
                throw new InvalidOperationException($"Unknown production method: {e.ProductionMethod}");
            elements.Add(new DesignElement
            {
                SurfaceCode = e.SurfaceCode,
                ProductionMethod = m,
                NormX = e.NormX,
                NormY = e.NormY,
                NormWidth = e.NormWidth,
                NormHeight = e.NormHeight
            });
        }

        await _integrity.ValidateCatalogOptionsAsync(
            product, req.FabricId, req.CutStyleId, req.ProductSizeId, req.PrintingOptionId, elements, ct);
        _integrity.NormalizeElementGeometry(product, elements);

        var priceInputs = elements
            .Select(e => new ElementPriceInput(e.SurfaceCode, e.ProductionMethod, e.RealWidth, e.RealHeight))
            .ToList();

        var result = await _pricing.CalculateDesignPriceAsync(new PriceCalculationRequest(
            req.ProductId, req.FabricId, req.CutStyleId, req.ProductSizeId, req.PrintingOptionId, priceInputs), ct);
        return Ok(ApiResponse<PriceBreakdown>.Ok(result));
    }
}

[ApiController]
[Authorize]
[Route("api/uploads")]
[EnableRateLimiting("upload")]
public class UploadsController : ControllerBase
{
    private readonly ZouqDbContext _db;
    private readonly IFileStorageService _files;
    private readonly AiUploadProcessor _aiProcessor;

    public UploadsController(ZouqDbContext db, IFileStorageService files, AiUploadProcessor aiProcessor)
    {
        _db = db; _files = files; _aiProcessor = aiProcessor;
    }

    private Guid UserId => Guid.Parse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [RequestSizeLimit(10_485_760)]
    public async Task<ActionResult<ApiResponse<object>>> Upload(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file uploaded."));
        if (file.Length > 10_485_760)
            return BadRequest(ApiResponse<object>.Fail("File exceeds 10MB limit."));

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant() ?? "";
        var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
        if (!allowedExt.Contains(ext))
            return BadRequest(ApiResponse<object>.Fail("Unsupported file extension."));

        var contentType = file.ContentType ?? "";
        if (!contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(ApiResponse<object>.Fail("Only image uploads are allowed."));

        await using var stream = file.OpenReadStream();
        var stored = await _files.SaveImageAsync(stream, Path.GetFileName(file.FileName), contentType, ct);

        var upload = new UserUpload
        {
            UserId = UserId,
            OriginalFileName = file.FileName,
            StoredFileName = stored.StoredFileName,
            FileUrl = stored.RelativeUrl,
            ContentType = contentType,
            FileSizeBytes = stored.SizeBytes,
            AiAnalysisStatus = "pending"
        };
        _db.UserUploads.Add(upload);
        await _db.SaveChangesAsync(ct);

        // AI is async — never blocks order/design creation
        _aiProcessor.Enqueue(upload.Id);

        return Ok(ApiResponse<object>.Ok(new
        {
            upload.Id,
            upload.FileUrl,
            upload.AiTags,
            upload.AiAnalysisStatus,
            upload.FileSizeBytes
        }));
    }

    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<object>>> Mine(CancellationToken ct)
    {
        var list = await _db.UserUploads.AsNoTracking()
            .Where(u => u.UserId == UserId && !u.IsDeleted)
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new { u.Id, u.FileUrl, u.OriginalFileName, u.AiTags, u.AiAnalysisStatus, u.FileSizeBytes, u.CreatedAtUtc })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(list));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Get(Guid id, CancellationToken ct)
    {
        var u = await _db.UserUploads.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == UserId && !x.IsDeleted, ct)
            ?? throw new UnauthorizedAccessException("Upload not found or not owned by you.");
        return Ok(ApiResponse<object>.Ok(new { u.Id, u.FileUrl, u.AiTags, u.AiAnalysisStatus, u.AiProvider, u.AiModel, u.AiError }));
    }
}

[ApiController]
[Authorize]
[Route("api/orders")]
[EnableRateLimiting("mutate")]
public class OrdersController : ControllerBase
{
    private readonly OrderAppService _orders;
    public OrdersController(OrderAppService orders) => _orders = orders;
    private Guid UserId => Guid.Parse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> Create([FromBody] CreateOrderRequest req, CancellationToken ct)
        => Ok(ApiResponse<OrderDto>.Ok(await _orders.CreateFromDesignAsync(UserId, req, ct)));

    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrderDto>>>> Mine(CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<OrderDto>>.Ok(await _orders.MyOrdersAsync(UserId, ct)));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderDetailDto>>> Get(Guid id, CancellationToken ct)
        => Ok(ApiResponse<OrderDetailDto>.Ok(await _orders.GetMineAsync(UserId, id, ct)));
}

[ApiController]
[Route("api/feed")]
[EnableRateLimiting("feed")]
public class FeedController : ControllerBase
{
    private readonly FeedAppService _feed;
    public FeedController(FeedAppService feed) => _feed = feed;

    [HttpGet("for-you")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FeedItemDto>>>> ForYou(
        [FromQuery] int take = 40, [FromQuery] int skip = 0, CancellationToken ct = default)
    {
        Guid? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            var raw = User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(raw, out var id)) userId = id;
        }
        return Ok(ApiResponse<IReadOnlyList<FeedItemDto>>.Ok(await _feed.ForYouAsync(userId, take, skip, ct)));
    }

    [HttpGet("ads")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdDto>>>> Ads(CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<AdDto>>.Ok(await _feed.ActiveAdsAsync(ct)));
}

[ApiController]
[Authorize]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly ZouqDbContext _db;
    public MeController(ZouqDbContext db) => _db = db;
    private Guid UserId => Guid.Parse(User.FindFirstValue("id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserDto>>> Profile(CancellationToken ct)
    {
        var u = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == UserId, ct);
        return Ok(ApiResponse<UserDto>.Ok(AuthAppService.MapUser(u)));
    }

    [HttpGet("interests")]
    public async Task<ActionResult<ApiResponse<object>>> GetInterests(CancellationToken ct)
    {
        var tags = await _db.UserInterestTags.AsNoTracking()
            .Where(t => t.UserId == UserId)
            .OrderBy(t => t.Tag)
            .Select(t => t.Tag)
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { tags }));
    }

    [HttpPut("interests")]
    public async Task<ActionResult<ApiResponse<object>>> SetInterests([FromBody] InterestsBody body, CancellationToken ct)
    {
        var incoming = (body.Tags ?? new List<string>())
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct()
            .Take(50)
            .ToList();

        var existing = await _db.UserInterestTags.Where(t => t.UserId == UserId).ToListAsync(ct);
        _db.UserInterestTags.RemoveRange(existing);
        foreach (var tag in incoming)
            _db.UserInterestTags.Add(new UserInterestTag { UserId = UserId, Tag = tag });
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { tags = incoming }));
    }

    [HttpPost("interests/{tag}")]
    public async Task<ActionResult<ApiResponse<object>>> AddInterest(string tag, CancellationToken ct)
    {
        var normalized = tag.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return BadRequest(ApiResponse<object>.Fail("Tag required."));
        if (!await _db.UserInterestTags.AnyAsync(t => t.UserId == UserId && t.Tag == normalized, ct))
        {
            _db.UserInterestTags.Add(new UserInterestTag { UserId = UserId, Tag = normalized });
            await _db.SaveChangesAsync(ct);
        }
        return Ok(ApiResponse<object>.Ok(new { tag = normalized }));
    }

    [HttpDelete("interests/{tag}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveInterest(string tag, CancellationToken ct)
    {
        var normalized = tag.Trim().ToLowerInvariant();
        var rows = await _db.UserInterestTags.Where(t => t.UserId == UserId && t.Tag == normalized).ToListAsync(ct);
        _db.UserInterestTags.RemoveRange(rows);
        await _db.SaveChangesAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { removed = tag }));
    }

    [HttpGet("balance")]
    public async Task<ActionResult<ApiResponse<object>>> Balance(CancellationToken ct)
    {
        var u = await _db.Users.AsNoTracking().FirstAsync(x => x.Id == UserId, ct);
        var ledger = await _db.LedgerEntries.AsNoTracking()
            .Where(l => l.UserId == UserId)
            .OrderByDescending(l => l.CreatedAtUtc)
            .Take(50)
            .Select(l => new { l.EntryType, l.Amount, l.BalanceAfter, l.Reason, l.CreatedAtUtc })
            .ToListAsync(ct);
        return Ok(ApiResponse<object>.Ok(new { balance = u.Balance, ledger }));
    }

    public record InterestsBody(List<string>? Tags);
}
