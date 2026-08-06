using IAMS.Domain.Common;

namespace IAMS.Domain.Entities;

/// <summary>Immutable audit trail record capturing who changed what.</summary>
public sealed class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}