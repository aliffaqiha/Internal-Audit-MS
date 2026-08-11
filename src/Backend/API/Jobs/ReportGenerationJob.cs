using IAMS.Application.AuditReports;
using IAMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IAMS.Api.Jobs;

/// <summary>Background audit report PDF generation target (invoked by Hangfire).</summary>
public interface IReportGenerationJob
{
    Task GenerateAsync(Guid auditPlanId, CancellationToken cancellationToken = default);
}

public sealed class ReportGenerationJob : IReportGenerationJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReportGenerationJob> _logger;

    public ReportGenerationJob(IServiceScopeFactory scopeFactory, ILogger<ReportGenerationJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task GenerateAsync(Guid auditPlanId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        // Idempotent: a report already exists for the plan -> nothing to do.
        var hasReport = await db.AuditReports.AnyAsync(r => r.AuditPlanId == auditPlanId, cancellationToken);
        if (hasReport)
        {
            _logger.LogInformation(
                "Report for plan {PlanId} already exists; skipping background generation", auditPlanId);
            return;
        }

        await sender.Send(new GenerateAuditReportCommand(auditPlanId), cancellationToken);
        _logger.LogInformation("Background report generated for plan {PlanId}", auditPlanId);
    }
}
