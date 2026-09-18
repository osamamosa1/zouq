using Zouq.Domain.Entities;
using Zouq.Domain.Enums;

namespace Zouq.Application.Interfaces;

public interface IJwtTokenService
{
    int AccessTokenExpiresSeconds { get; }
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IFileStorageService
{
    Task<StoredFileResult> SaveImageAsync(Stream content, string originalFileName, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
}

public record StoredFileResult(string StoredFileName, string RelativeUrl, long SizeBytes);

public interface IAiImageAnalysisService
{
    /// <summary>Analyze an image URL. Must never throw for transient provider failures — return Status=failed.</summary>
    Task<AiAnalysisResult> AnalyzeAsync(string imageUrl, CancellationToken ct = default);
}

public record AiAnalysisResult(
    IReadOnlyList<string> Tags,
    string Provider,
    string? Model,
    string Status,
    string? Error = null);

public interface IPriceCalculationService
{
    Task<PriceBreakdown> CalculateDesignPriceAsync(PriceCalculationRequest request, CancellationToken ct = default);
}

public record PriceCalculationRequest(
    Guid ProductId,
    Guid? FabricId,
    Guid? CutStyleId,
    Guid? ProductSizeId,
    Guid? PrintingOptionId,
    IReadOnlyList<ElementPriceInput> Elements);

public record ElementPriceInput(
    string SurfaceCode,
    ProductionMethodCode ProductionMethod,
    decimal RealWidth,
    decimal RealHeight);

public record PriceBreakdown(
    decimal BaseProductPrice,
    decimal FabricAdjustment,
    decimal CutAdjustment,
    decimal SizeAdjustment,
    decimal PrintingCost,
    decimal EmbroideryCost,
    decimal Total,
    IReadOnlyList<PriceLine> Lines);

public record PriceLine(string Code, string Label, decimal Amount);
