using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using IAMS.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Findings;

/// <summary>
/// Reacts to <see cref="FindingCreatedEvent"/>: notifies the affected auditees in the
/// finding's department (via the email service, which logs in dev) and records an
/// audit trail entry. Keeping this side-effect out of the create handler keeps the
/// command focused and side effects decoupled.
/// </summary>
internal sealed class FindingCreatedHandler : INotificationHandler<FindingCreatedEvent>
{
    private readonly IApplicationDbContext _db;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;

    public FindingCreatedHandler(IApplicationDbContext db, IEmailService email, IAuditService audit)
    {
        _db = db;
        _email = email;
        _audit = audit;
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
            .Select(u => new { u.Email, u.FullName })
            .ToListAsync(cancellationToken);

        var riskLabel = notification.RiskLevel.ToString();
        var subject = $"[IAMS] Temuan baru: {notification.Title}";
        var body =
            $"Temuan audit berisiko {riskLabel} telah dicatat untuk departemen Anda.\n\n" +
            $"Judul: {notification.Title}\n" +
            (notification.DueDate.HasValue
                ? $"Tenggat: {notification.DueDate.Value:yyyy-MM-dd}\n"
                : "") +
            "\nSilakan lihat detail temuan pada aplikasi Internal Audit.";

        foreach (var recipient in recipients)
            await _email.SendAsync(recipient.Email, subject, body, cancellationToken);

        await _audit.LogAsync("Notification.Sent", nameof(Finding), notification.FindingId.ToString(),
            newValues: $"auditees_notified={recipients.Count}",
            cancellationToken: cancellationToken);
    }
}