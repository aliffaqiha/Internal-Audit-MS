using IAMS.Application.Common.Interfaces;

namespace IAMS.Api.Jobs;

/// <summary>Testing fallback: never enqueues background jobs.</summary>
public sealed class NoopAuditReportJobQueue : IAuditReportJobQueue
{
    public Task EnqueueGenerateReportAsync(Guid auditPlanId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
