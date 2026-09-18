using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Zouq.Application.Interfaces;

namespace Zouq.Infrastructure.Services;

/// <summary>Deterministic stub — useful for local/dev and when Ai:Provider=stub.</summary>
public class StubAiImageAnalysisService : IAiImageAnalysisService
{
    public Task<AiAnalysisResult> AnalyzeAsync(string imageUrl, CancellationToken ct = default)
    {
        // Intentionally empty tags — never invent fake product tags as if AI ran.
        IReadOnlyList<string> tags = Array.Empty<string>();
        return Task.FromResult(new AiAnalysisResult(tags, "stub", "none", "skipped", "AI stub — configure Ai:Provider=openai for production analysis."));
    }
}

/// <summary>
/// OpenAI-compatible vision/chat provider. Failures return Status=failed — callers must not abort orders/uploads.
/// </summary>
public class OpenAiCompatibleAiImageAnalysisService : IAiImageAnalysisService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<OpenAiCompatibleAiImageAnalysisService> _logger;

    public OpenAiCompatibleAiImageAnalysisService(
        HttpClient http,
        IConfiguration config,
        ILogger<OpenAiCompatibleAiImageAnalysisService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<AiAnalysisResult> AnalyzeAsync(string imageUrl, CancellationToken ct = default)
    {
        var apiKey = _config["Ai:ApiKey"];
        var model = _config["Ai:Model"] ?? "gpt-4o-mini";
        var baseUrl = (_config["Ai:BaseUrl"] ?? "https://api.openai.com/v1").TrimEnd('/');

        if (string.IsNullOrWhiteSpace(apiKey))
            return new AiAnalysisResult(Array.Empty<string>(), "openai", model, "failed", "Ai:ApiKey is not configured.");

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var absoluteUrl = imageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? imageUrl
                : (_config["Ai:PublicBaseUrl"]?.TrimEnd('/') ?? "") + imageUrl;

            var body = new
            {
                model,
                temperature = 0.2,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "You analyze product design images. Return ONLY a JSON object {\"tags\":[\"...\"]} with 5-12 short lowercase English tags covering themes, objects, style, colors. No markdown."
                    },
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = "Tag this design image for a personalized product feed." },
                            new { type = "image_url", image_url = new { url = absoluteUrl } }
                        }
                    }
                }
            };

            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            using var resp = await _http.SendAsync(req, ct);
            var json = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("AI provider HTTP {Status}: {Body}", (int)resp.StatusCode, Truncate(json));
                return new AiAnalysisResult(Array.Empty<string>(), "openai", model, "failed", $"HTTP {(int)resp.StatusCode}");
            }

            using var doc = JsonDocument.Parse(json);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";

            var tags = ParseTags(content);
            return new AiAnalysisResult(tags, "openai", model, "completed");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "AI analysis failed for {Url}", imageUrl);
            return new AiAnalysisResult(Array.Empty<string>(), "openai", model, "failed", ex.Message);
        }
    }

    private static IReadOnlyList<string> ParseTags(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
                trimmed = trimmed[start..(end + 1)];
        }

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.TryGetProperty("tags", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                return arr.EnumerateArray()
                    .Select(x => x.GetString()?.Trim().ToLowerInvariant())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(20)
                    .Cast<string>()
                    .ToList();
            }
        }
        catch
        {
            // fall through to token split
        }

        return trimmed.Split(new[] { ',', ' ', '\n', '#', '"' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => t.Length is > 1 and < 40)
            .Distinct()
            .Take(12)
            .ToList();
    }

    private static string Truncate(string s) => s.Length <= 400 ? s : s[..400];
}

/// <summary>Resolves stub vs openai from Ai:Provider.</summary>
public class ConfigurableAiImageAnalysisService : IAiImageAnalysisService
{
    private readonly IAiImageAnalysisService _inner;

    public ConfigurableAiImageAnalysisService(
        IConfiguration config,
        IHttpClientFactory httpFactory,
        ILoggerFactory loggerFactory)
    {
        var provider = (config["Ai:Provider"] ?? "stub").Trim().ToLowerInvariant();
        _inner = provider switch
        {
            "openai" => new OpenAiCompatibleAiImageAnalysisService(
                httpFactory.CreateClient("zouq-ai"),
                config,
                loggerFactory.CreateLogger<OpenAiCompatibleAiImageAnalysisService>()),
            _ => new StubAiImageAnalysisService()
        };
    }

    public Task<AiAnalysisResult> AnalyzeAsync(string imageUrl, CancellationToken ct = default)
        => _inner.AnalyzeAsync(imageUrl, ct);
}
