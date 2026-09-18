using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zouq.Application.Interfaces;
using Zouq.Infrastructure.Data;

namespace Zouq.Infrastructure.Services;

/// <summary>
/// Fire-and-forget AI tagging for uploads. Order/design flows never wait on this.
/// </summary>
public class AiUploadProcessor
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<AiUploadProcessor> _logger;

    public AiUploadProcessor(IServiceScopeFactory scopes, ILogger<AiUploadProcessor> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public void Enqueue(Guid uploadId)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ZouqDbContext>();
                var ai = scope.ServiceProvider.GetRequiredService<IAiImageAnalysisService>();
                var upload = await db.UserUploads.FirstOrDefaultAsync(u => u.Id == uploadId);
                if (upload == null) return;

                upload.AiAnalysisStatus = "processing";
                await db.SaveChangesAsync();

                var result = await ai.AnalyzeAsync(upload.FileUrl);
                upload.AiTags = result.Tags.ToList();
                upload.AiProvider = result.Provider;
                upload.AiModel = result.Model;
                upload.AiAnalysisStatus = result.Status;
                upload.AiError = result.Error;
                upload.AiProcessedAtUtc = DateTime.UtcNow;

                // Merge AI tags onto draft designs that already reference this upload (best-effort)
                if (result.Status == "completed" && result.Tags.Count > 0)
                {
                    var designIds = await db.DesignElements.AsNoTracking()
                        .Where(e => e.UserUploadId == uploadId)
                        .Select(e => e.DesignId)
                        .Distinct()
                        .ToListAsync();
                    var designs = await db.Designs.Where(d => designIds.Contains(d.Id)).ToListAsync();
                    foreach (var d in designs)
                    {
                        var merged = d.Tags
                            .Concat(result.Tags)
                            .Select(t => t.Trim().ToLowerInvariant())
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .Distinct()
                            .Take(40)
                            .ToList();
                        d.Tags = merged;
                    }
                }

                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background AI processing failed for upload {UploadId}", uploadId);
            }
        });
    }
}
