using Hangfire;
using IAMS.Application.Common.Interfaces;

namespace IAMS.Api.Jobs;

/// <summary>Fire-and-forget report generation via the Hangfire default queue.</summary>
public sealed class HangfireAuditReportJobQueue : IAuditReportJobQueue
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireAuditReportJobQueue(IBackgroundJobClient jobs)
    {
        _jobs = jobs;
    }

    public Task EnqueueGenerateReportAsync(Guid auditPlanId, CancellationToken cancellationToken = default)
    {
        _jobs.Enqueue<IReportGenerationJob>(job => job.GenerateAsync(auditPlanId, CancellationToken.None));
        return Task.CompletedTask;
    }
}
