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

    public GetAuditReportQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<AuditReportDto?> Handle(GetAuditReportQuery request, CancellationToken cancellationToken)
    {
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
