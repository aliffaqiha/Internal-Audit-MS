using IAMS.Domain.Common;
using IAMS.Domain.Enums;

namespace IAMS.Domain.Entities;

public sealed class AuditChecklistItem : BaseEntity
{
    public Guid AuditPlanId { get; set; }
    public AuditPlan? AuditPlan { get; set; }

    public string Question { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsRequired { get; set; } = true;

    public ChecklistItemStatus Status { get; set; } = ChecklistItemStatus.Pending;
    public string? Note { get; set; }
}