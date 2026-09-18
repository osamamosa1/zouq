using System.ComponentModel.DataAnnotations;
using Zouq.Domain.Enums;

namespace Zouq.Application.DTOs;

public record AuthRegisterRequest(
    [Required] string Name,
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password,
    string? Phone);

public record AuthLoginRequest(
    [Required] string Email,
    [Required] string Password);

public record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserDto User);

public record RefreshTokenRequest(string RefreshToken);

public record UserDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    string Role,
    string? AvatarUrl,
    decimal Balance);

public record ProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    string? ThumbnailUrl,
    decimal BasePrice,
    string ProductTypeName,
    string MeasurementUnit);

public record ProductConfigDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ThumbnailUrl,
    decimal BasePrice,
    string MeasurementUnit,
    string ProductTypeName,
    IReadOnlyList<SurfaceDto> Surfaces,
    IReadOnlyList<FabricDto> Fabrics,
    IReadOnlyList<CutStyleDto> Cuts,
    IReadOnlyList<SizeDto> Sizes,
    IReadOnlyList<PrintingOptionDto> PrintingOptions,
    EmbroideryPricingDto? EmbroideryPricing);

public record SurfaceDto(
    Guid Id,
    string Code,
    string Name,
    string? PreviewImageUrl,
    bool IsRequired,
    DesignAreaDto? DesignArea);

public record DesignAreaDto(
    double NormX,
    double NormY,
    double NormWidth,
    double NormHeight,
    decimal RealWidth,
    decimal RealHeight);

public record FabricDto(Guid Id, string Name, string? Description, string? ImageUrl, decimal PriceAdjustment);
public record CutStyleDto(Guid Id, string Name, string? Description, string? ImageUrl, decimal PriceAdjustment);
public record SizeDto(Guid Id, string Code, string Name, decimal Width, decimal Height, decimal? Depth, string Unit, decimal PriceAdjustment);
public record PrintingOptionDto(Guid Id, string Name, string Code, string? Description, decimal Price, IReadOnlyList<string> IncludedSurfaceCodes);
public record EmbroideryPricingDto(decimal PricePerSquareUnit, string Unit, decimal? MinimumCharge);

public record DesignElementInputDto(
    string SurfaceCode,
    Guid? DesignAssetId,
    Guid? UserUploadId,
    string ProductionMethod,
    double NormX,
    double NormY,
    double NormWidth,
    double NormHeight,
    double RotationDegrees,
    double Scale,
    decimal RealWidth,
    decimal RealHeight,
    int ZIndex);

public record SaveDesignRequest(
    Guid ProductId,
    Guid? FabricId,
    Guid? CutStyleId,
    Guid? ProductSizeId,
    Guid? PrintingOptionId,
    string Title,
    string? Description,
    string? Status,
    string? Visibility,
    IReadOnlyList<DesignElementInputDto> Elements);

public record DesignDto(
    Guid Id,
    string Title,
    string? Description,
    string Status,
    string Visibility,
    bool IsFeatured,
    string? PreviewImageUrl,
    Guid ProductId,
    Guid? FabricId,
    Guid? CutStyleId,
    Guid? ProductSizeId,
    Guid? PrintingOptionId,
    decimal? LastEstimatedPrice,
    IReadOnlyList<string> Tags,
    IReadOnlyList<DesignElementDto> Elements,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    Guid? DerivedFromDesignId = null);

public record DesignElementDto(
    Guid Id,
    string SurfaceCode,
    Guid? DesignAssetId,
    Guid? UserUploadId,
    string ProductionMethod,
    double NormX,
    double NormY,
    double NormWidth,
    double NormHeight,
    double RotationDegrees,
    double Scale,
    decimal RealWidth,
    decimal RealHeight,
    int ZIndex);

public record QuotePriceRequest(
    Guid ProductId,
    Guid? FabricId,
    Guid? CutStyleId,
    Guid? ProductSizeId,
    Guid? PrintingOptionId,
    IReadOnlyList<ElementQuoteDto> Elements);

/// <summary>Client sends normalized geometry only — server computes real cm.</summary>
public record ElementQuoteDto(
    string SurfaceCode,
    string ProductionMethod,
    double NormX,
    double NormY,
    double NormWidth,
    double NormHeight);

public record CreateOrderRequest(
    Guid DesignId,
    int Quantity,
    string? ShippingAddressJson,
    string? Notes);

public record OrderDto(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal Subtotal,
    decimal Total,
    string Currency,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemDto> Items);

public record OrderItemDto(
    Guid Id,
    Guid? SourceDesignId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>Customer-facing immutable order detail with snapshot JSON payloads.</summary>
public record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    string Status,
    decimal Subtotal,
    decimal Total,
    string Currency,
    DateTime CreatedAtUtc,
    decimal CommissionPercentSnapshot,
    decimal? CreatorRewardAmount,
    IReadOnlyList<OrderDetailItemDto> Items);

public record OrderDetailItemDto(
    Guid Id,
    Guid? SourceDesignId,
    Guid? DesignerId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal,
    string DesignSnapshotJson,
    string PricingBreakdownJson,
    string ProductSnapshotJson);

public record FeedItemDto(
    Guid DesignId,
    string Title,
    string? PreviewImageUrl,
    string OwnerName,
    bool IsFeatured,
    int FeaturedPriority,
    IReadOnlyList<string> Tags,
    decimal? PriceHint);

public record AdDto(
    Guid Id,
    string Title,
    string? Description,
    string? ImageUrl,
    string? LinkUrl,
    string? ActionType,
    string Placement,
    int DisplayPriority);

public record DesignAssetDto(
    Guid Id,
    string Name,
    string FileUrl,
    string? ThumbnailUrl,
    string? CategoryName,
    IReadOnlyList<string> Tags);
