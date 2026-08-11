using IAMS.Domain.Common;
using MediatR;

namespace IAMS.Domain.Events;

public sealed class AuditPlanApprovedEvent : DomainEvent, INotification
{
    public AuditPlanApprovedEvent(Guid auditPlanId, string title)
    {
        AuditPlanId = auditPlanId;
        Title = title;
    }

    public Guid AuditPlanId { get; }
    public string Title { get; }
}