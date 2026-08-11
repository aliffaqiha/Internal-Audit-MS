namespace IAMS.Application.Common.Interfaces;

/// <summary>Fire-and-forget queue for background audit report generation (Hangfire-backed).</summary>
public interface IAuditReportJobQueue
{
    Task EnqueueGenerateReportAsync(Guid auditPlanId, CancellationToken cancellationToken = default);
}
