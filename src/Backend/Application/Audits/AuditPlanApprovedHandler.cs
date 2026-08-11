using IAMS.Application.Common.Interfaces;
using IAMS.Application.Notifications;
using IAMS.Domain.Enums;
using IAMS.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Audits;

/// <summary>
/// Reacts to <see cref="AuditPlanApprovedEvent"/>: notifies the audit team assigned to
/// the plan that the plan has been approved and work can begin.
/// </summary>
internal sealed class AuditPlanApprovedHandler : INotificationHandler<AuditPlanApprovedEvent>
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationService _notifications;

    public AuditPlanApprovedHandler(IApplicationDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public async Task Handle(AuditPlanApprovedEvent notification, CancellationToken cancellationToken)
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
            NotificationType.AuditApproved,
            "Rencana audit disetujui",
            $"Rencana audit \"{notification.Title}\" telah disetujui dan siap untuk dilaksanakan.",
            $"/audits/{notification.AuditPlanId}",
            cancellationToken: cancellationToken);
    }
}