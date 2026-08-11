using IAMS.Application.Common.Interfaces;
using IAMS.Application.Notifications;
using IAMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IAMS.Infrastructure.Services;

public sealed class ReminderOptions
{
    public const string SectionName = "Reminders";

    /// <summary>Hours between CAP due/overdue checks. Defaults to 6.</summary>
    public int CapCheckIntervalHours { get; set; } = 6;
}

/// <summary>Scans CAP due/overdue and notifies the affected auditees. Scheduled by Hangfire.</summary>
public interface ICapReminderService
{
    Task RunOnceAsync(CancellationToken cancellationToken = default);
}

public sealed class CapReminderService : ICapReminderService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CapReminderService> _logger;

    public CapReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<CapReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var today = DateTimeOffset.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var caps = await db.CorrectiveActions
            .AsNoTracking()
            .Where(c => c.TargetDate.HasValue
                        && c.Status != CorrectiveActionStatus.Closed)
            .Select(c => new
            {
                c.Id,
                c.FindingId,
                c.Action,
                TargetDate = c.TargetDate!.Value,
                DeptId = c.Finding != null ? c.Finding.DepartmentId : (Guid?)null
            })
            .ToListAsync(cancellationToken);

        int notifiedDue = 0, notifiedOverdue = 0;

        foreach (var cap in caps)
        {
            var dueDate = cap.TargetDate.Date;

            string title;
            string message;
            string dedupeKey;
            bool isOverdue;

            if (dueDate == tomorrow)
            {
                title = "CAP berbatas waktu besok";
                message = $"CAP \"{cap.Action}\" akan jatuh tempo besok ({dueDate:yyyy-MM-dd}).";
                dedupeKey = $"cap-due:{cap.Id}:{dueDate:yyyyMMdd}";
                isOverdue = false;
            }
            else if (dueDate <= today)
            {
                title = "CAP melewati tenggat";
                message = $"CAP \"{cap.Action}\" telah melewati tenggat ({dueDate:yyyy-MM-dd}). Segera selesaikan.";
                dedupeKey = $"cap-overdue:{cap.Id}:{today:yyyyMMdd}";
                isOverdue = true;
            }
            else
            {
                continue;
            }

            var recipients = await db.Users
                .AsNoTracking()
                .Where(u => u.DepartmentId == cap.DeptId
                            && u.IsActive
                            && u.UserRoles.Any(ur => ur.Role.NormalizedName == RoleConstants.Normalize(RoleConstants.Auditee)))
                .Select(u => new { u.Id, u.Email })
                .ToListAsync(cancellationToken);

            if (recipients.Count == 0)
                continue;

            await notifications.SendAsync(
                recipients.Select(r => r.Id).ToList(),
                NotificationType.CapReminder,
                title,
                message,
                $"/caps/finding/{cap.FindingId}",
                dedupeKey,
                cancellationToken);

            foreach (var recipient in recipients)
                await email.SendAsync(recipient.Email, $"[IAMS] {title}", message, cancellationToken);

            if (isOverdue) notifiedOverdue++;
            else notifiedDue++;
        }

        if (notifiedDue > 0 || notifiedOverdue > 0)
            _logger.LogInformation("CAP reminders sent -> dueTomorrow={Due} overdue={Overdue}", notifiedDue, notifiedOverdue);
    }
}