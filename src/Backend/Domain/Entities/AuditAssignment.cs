using IAMS.Domain.Common;

namespace IAMS.Domain.Entities;

public sealed class AuditAssignment : BaseEntity
{
    public Guid AuditPlanId { get; set; }
    public AuditPlan? AuditPlan { get; set; }

    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Position within the team, e.g. "Lead Auditor" or "Auditor".</summary>
    public string? RoleInPlan { get; set; }
}