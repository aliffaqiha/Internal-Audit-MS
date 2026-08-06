using IAMS.Domain.Common;

namespace IAMS.Domain.Entities;

/// <summary>Opaque refresh token with rotation support (revoked on use).</summary>
public sealed class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public Guid JwtId { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
    public string? CreatedByIp { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired => ExpiresAt <= DateTimeOffset.UtcNow;
    public bool IsActive => !IsRevoked && !IsExpired;
}