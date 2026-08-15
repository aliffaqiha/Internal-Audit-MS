using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.AuditReports;

/// <summary>Returns metadata of the latest generated report for an audit plan, or null.</summary>
public sealed record GetAuditReportQuery(Guid AuditPlanId) : IRequest<AuditReportDto?>;

internal sealed class GetAuditReportQueryHandler : IRequestHandler<GetAuditReportQuery, AuditReportDto?>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAuditReportQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AuditReportDto?> Handle(GetAuditReportQuery request, CancellationToken cancellationToken)
    {
        var scope = await CurrentUserAccess.ResolveAsync(_db, _currentUser, cancellationToken);

        var planDepartmentId = await _db.AuditPlans.AsNoTracking()
            .Where(p => p.Id == request.AuditPlanId)
            .Select(p => (Guid?)p.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (planDepartmentId is null)
            return null;

        CurrentUserAccess.EnsureCanAccessPlan(scope, planDepartmentId);

        var report = await _db.AuditReports.AsNoTracking()
            .Where(r => r.AuditPlanId == request.AuditPlanId)
            .Select(r => new AuditReportDto(
                r.Id,
                r.AuditPlanId,
                r.ObjectName,
                r.FileName,
                r.ContentType,
                r.SizeBytes,
                r.GeneratedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return report;
    }
}
