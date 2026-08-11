using IAMS.Application.Common.Interfaces;
using IAMS.Application.Notifications;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using IAMS.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Findings;

/// <summary>
/// Reacts to <see cref="FindingCreatedEvent"/>: raises an in-app notification and an
/// email for the affected auditees in the finding's department. Keeping this side-effect
/// out of the create handler keeps the command focused and side effects decoupled.
/// </summary>
internal sealed class FindingCreatedHandler : INotificationHandler<FindingCreatedEvent>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;
    private readonly INotificationService _notifications;

    public FindingCreatedHandler(
        IApplicationDbContext db,
        IEmailService email,
        IAuditService audit,
        INotificationService notifications)
    {
        _db = db;
        _email = email;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task Handle(FindingCreatedEvent notification, CancellationToken cancellationToken)
    {
        if (!notification.DepartmentId.HasValue)
        {
            await _audit.LogAsync("Notification.Skipped", nameof(Finding), notification.FindingId.ToString(),
                newValues: "no department to notify", cancellationToken: cancellationToken);
            return;
        }

        var recipients = await _db.Users
            .AsNoTracking()
            .Where(u => u.DepartmentId == notification.DepartmentId
                        && u.IsActive
                        && u.UserRoles.Any(ur => ur.Role.NormalizedName == RoleConstants.Normalize(RoleConstants.Auditee)))
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToListAsync(cancellationToken);

        if (recipients.Count == 0)
        {
            await _audit.LogAsync("Notification.Skipped", nameof(Finding), notification.FindingId.ToString(),
                newValues: "no auditee recipients", cancellationToken: cancellationToken);
            return;
        }

        var riskLabel = notification.RiskLevel.ToString();
        var subject = $"[IAMS] Temuan baru: {notification.Title}";
        var message =
            $"Temuan audit berisiko {riskLabel} telah dicatat untuk departemen Anda.\n\n" +
            $"Judul: {notification.Title}\n" +
            (notification.DueDate.HasValue
                ? $"Tenggat: {notification.DueDate.Value:yyyy-MM-dd}\n"
                : "") +
            "\nSilakan lihat detail temuan pada aplikasi Internal Audit.";

        foreach (var recipient in recipients)
        {
            await _email.SendAsync(recipient.Email, subject, message, cancellationToken);
            await _notifications.SendAsync(
                new[] { recipient.Id },
                NotificationType.FindingAssigned,
                "Temuan baru untuk departemen Anda",
                $"Temuan berisiko {riskLabel} dicatat: {notification.Title}",
                $"/findings/{notification.FindingId}",
                cancellationToken: cancellationToken);
        }

        await _audit.LogAsync("Notification.Sent", nameof(Finding), notification.FindingId.ToString(),
            newValues: $"auditees_notified={recipients.Count}",
            cancellationToken: cancellationToken);
    }
}