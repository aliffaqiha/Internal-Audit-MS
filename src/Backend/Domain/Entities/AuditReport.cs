using IAMS.Domain.Common;

namespace IAMS.Domain.Entities;

/// <summary>Metadata of the latest generated audit report PDF stored in object storage.</summary>
public sealed class AuditReport : BaseEntity
{
    public Guid AuditPlanId { get; set; }
    public AuditPlan? AuditPlan { get; set; }

    public string ObjectName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long SizeBytes { get; set; }
    public DateTimeOffset GeneratedAt { get; set; }
}
