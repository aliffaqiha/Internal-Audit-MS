using IAMS.Domain.Common;
using IAMS.Domain.Enums;

namespace IAMS.Domain.Entities;

public sealed class AuditPlan : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Objective { get; set; }
    public string? Scope { get; set; }
    public string? Standard { get; set; }

    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }

    public AuditPlanStatus Status { get; set; } = AuditPlanStatus.Draft;
    public string? RejectionReason { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public ICollection<AuditAssignment> Assignments { get; set; } = new List<AuditAssignment>();
    public ICollection<AuditChecklistItem> ChecklistItems { get; set; } = new List<AuditChecklistItem>();
}