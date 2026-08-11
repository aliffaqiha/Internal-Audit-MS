using IAMS.Domain.Common;

namespace IAMS.Domain.Entities;

/// <summary>In-app notification delivered to a user (paired with an optional email).</summary>
public sealed class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? Link { get; set; }

    /// <summary>Deduplication key used by scheduled jobs (e.g. "cap-overdue:&lt;capId&gt;:&lt;date&gt;").</summary>
    public string? DedupeKey { get; set; }

    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}