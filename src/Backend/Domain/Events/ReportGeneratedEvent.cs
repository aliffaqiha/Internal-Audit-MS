using IAMS.Domain.Common;
using MediatR;

namespace IAMS.Domain.Events;

/// <summary>Raised after an audit report PDF has been generated and stored.</summary>
public sealed class ReportGeneratedEvent : DomainEvent, INotification
{
    public ReportGeneratedEvent(Guid auditPlanId, string title)
    {
        AuditPlanId = auditPlanId;
        Title = title;
    }

    public Guid AuditPlanId { get; }
    public string Title { get; }
}
