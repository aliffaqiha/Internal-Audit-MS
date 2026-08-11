using IAMS.Application.Common.Interfaces;
using IAMS.Application.Notifications;
using IAMS.Domain.Enums;
using IAMS.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.AuditReports;

/// <summary>Notifies the audit team once a report has been generated.</summary>
internal sealed class ReportGeneratedHandler : INotificationHandler<ReportGeneratedEvent>
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public ReportGeneratedHandler(IApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task Handle(ReportGeneratedEvent notification, CancellationToken cancellationToken)
    {
        var memberIds = await _db.AuditAssignments
            .AsNoTracking()
            .Where(a => a.AuditPlanId == notification.AuditPlanId)
            .Select(a => a.UserId)
            .ToListAsync(cancellationToken);

        if (memberIds.Count == 0)
            return;

        await _notifications.SendAsync(
            memberIds,
            NotificationType.ReportReady,
            "Laporan audit siap",
            $"Laporan untuk rencana audit \"{notification.Title}\" telah dibuat.",
            $"/audits/{notification.AuditPlanId}",
            cancellationToken: cancellationToken);
    }
}
