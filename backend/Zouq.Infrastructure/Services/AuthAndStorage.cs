using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
// IConfiguration from Microsoft.Extensions.Configuration
using Zouq.Application.Interfaces;
using Zouq.Domain.Entities;

namespace Zouq.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    public int AccessTokenExpiresSeconds { get; }

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        AccessTokenExpiresSeconds = int.TryParse(configuration["Jwt:ExpiresHours"], out var hours)
            ? hours * 3600
            : 24 * 3600;
    }

    public string GenerateAccessToken(User user)
    {
        var jwt = _configuration.GetSection("Jwt");
        var keyStr = jwt["Key"] ?? "Zouq_Jwt_SuperSecret_Key_ChangeMe_2026!";
        var key = Encoding.UTF8.GetBytes(keyStr);
        if (key.Length < 32)
            key = SHA256.HashData(Encoding.UTF8.GetBytes(keyStr));

        var claims = new List<Claim>
        {
            new("id", user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("role", user.Role.ToString()),
            new("email", user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(AccessTokenExpiresSeconds),
            Issuer = jwt["Issuer"],
            Audience = jwt["Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(48);
        return Convert.ToBase64String(bytes);
    }
}

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(hash)) return false;
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch { return false; }
    }
}

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _root;
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/svg+xml"
    };

    public LocalFileStorageService(IConfiguration configuration)
    {
        _root = configuration["Storage:Root"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
        Directory.CreateDirectory(_root);
    }

    public async Task<StoredFileResult> SaveImageAsync(Stream content, string originalFileName, string contentType, CancellationToken ct = default)
    {
        if (!AllowedContentTypes.Contains(contentType))
            throw new InvalidOperationException("Unsupported file type.");

        const long maxBytes = 10 * 1024 * 1024;
        if (content.CanSeek && content.Length > maxBytes)
            throw new InvalidOperationException("File exceeds 10MB limit.");

        var ext = Path.GetExtension(originalFileName)?.ToLowerInvariant() ?? "";
        var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
        if (!allowedExt.Contains(ext))
        {
            ext = contentType.ToLowerInvariant() switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/svg+xml" => ".svg",
                "image/jpeg" or "image/jpg" => ".jpg",
                _ => throw new InvalidOperationException("Unsupported file type.")
            };
        }

        var stored = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(_root, stored);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);
        var size = fs.Length;
        if (size > maxBytes)
        {
            fs.Close();
            File.Delete(fullPath);
            throw new InvalidOperationException("File exceeds 10MB limit.");
        }

        return new StoredFileResult(stored, $"/uploads/{stored}", size);
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var name = Path.GetFileName(relativePath);
        var full = Path.Combine(_root, name);
        if (File.Exists(full)) File.Delete(full);
        return Task.CompletedTask;
    }
}
