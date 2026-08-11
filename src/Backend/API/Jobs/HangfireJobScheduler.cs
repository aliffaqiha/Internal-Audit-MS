using Hangfire;
using IAMS.Infrastructure.Services;

namespace IAMS.Api.Jobs;

/// <summary>Registers idempotent recurring cron jobs at startup (same job id upserts, never duplicates).</summary>
public sealed class HangfireJobScheduler : IHostedService
{
    private readonly ICapReminderService _reminders;
    private readonly ICleanupService _cleanup;

    public HangfireJobScheduler(ICapReminderService reminders, ICleanupService cleanup)
    {
        _reminders = reminders;
        _cleanup = cleanup;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Every 6 hours: scan CAP due/overdue and notify auditees (dedupe key keeps it idempotent).
        RecurringJob.AddOrUpdate(
            "cap-reminder",
            () => _reminders.RunOnceAsync(CancellationToken.None),
            Cron.HourInterval(6));

        // Daily 03:00: purge stale refresh/password-reset tokens.
        RecurringJob.AddOrUpdate(
            "token-cleanup",
            () => _cleanup.CleanupAsync(CancellationToken.None),
            Cron.Daily(3));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
