using IAMS.Domain.Common;

namespace IAMS.Domain.Entities;

/// <summary>Single-use password reset token (stored hashed).</summary>
public sealed class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public string? CreatedByIp { get; set; }
}