using Zouq.Domain.Common;
using Zouq.Domain.Enums;

namespace Zouq.Domain.Entities;

public class User : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Cached wallet total — ledger is source of truth; protected by concurrency token.</summary>
    public decimal Balance { get; set; }

    /// <summary>Optimistic concurrency token to prevent lost updates on Balance.</summary>
    public ulong ConcurrencyStamp { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserInterestTag> InterestTags { get; set; } = new List<UserInterestTag>();
}

public class RefreshToken : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public bool IsRevoked { get; set; }
}

public class UserInterestTag : AuditableEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Tag { get; set; } = string.Empty;
    public double Weight { get; set; } = 1;
}
