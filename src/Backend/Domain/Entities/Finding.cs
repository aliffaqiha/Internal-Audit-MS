using IAMS.Domain.Common;
using IAMS.Domain.Enums;

namespace IAMS.Domain.Entities;

/// <summary>Audit finding with an associated risk rating, recommendation and evidence.</summary>
public sealed class Finding : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public RiskLevel RiskLevel { get; set; } = RiskLevel.Medium;
    public string? Category { get; set; }
    public string? Recommendation { get; set; }
    public DateTimeOffset? DueDate { get; set; }

    public Guid? AuditPlanId { get; set; }
    public AuditPlan? AuditPlan { get; set; }

    public ICollection<FindingEvidence> Evidences { get; set; } = new List<FindingEvidence>();
}