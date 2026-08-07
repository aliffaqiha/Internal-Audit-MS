using IAMS.Domain.Common;
using IAMS.Domain.Enums;
using MediatR;

namespace IAMS.Domain.Events;

public sealed class FindingCreatedEvent : DomainEvent, INotification
{
    public FindingCreatedEvent(
        Guid findingId,
        string title,
        RiskLevel riskLevel,
        DateTimeOffset? dueDate,
        Guid? departmentId)
    {
        FindingId = findingId;
        Title = title;
        RiskLevel = riskLevel;
        DueDate = dueDate;
        DepartmentId = departmentId;
    }

    public Guid FindingId { get; }
    public string Title { get; }
    public RiskLevel RiskLevel { get; }
    public DateTimeOffset? DueDate { get; }
    public Guid? DepartmentId { get; }
}