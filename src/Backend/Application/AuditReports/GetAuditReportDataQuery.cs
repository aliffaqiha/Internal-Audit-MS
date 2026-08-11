using IAMS.Application.Common.Interfaces;
using IAMS.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.AuditReports;

/// <summary>Collects the full data set required to render an audit report PDF.</summary>
public sealed record GetAuditReportDataQuery(Guid AuditPlanId) : IRequest<AuditReportDataDto>;

internal sealed class GetAuditReportDataQueryHandler : IRequestHandler<GetAuditReportDataQuery, AuditReportDataDto>
{
    private readonly IApplicationDbContext _db;

    public GetAuditReportDataQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<AuditReportDataDto> Handle(GetAuditReportDataQuery request, CancellationToken cancellationToken)
    {
        var plan = await _db.AuditPlans.AsNoTracking()
            .Include(p => p.Department)
            .Include(p => p.Assignments).ThenInclude(a => a.User)
            .Include(p => p.ChecklistItems)
            .FirstOrDefaultAsync(p => p.Id == request.AuditPlanId, cancellationToken)
            ?? throw new KeyNotFoundException("Rencana audit tidak ditemukan.");

        if (plan.Status == Domain.Enums.AuditPlanStatus.Draft
            || plan.Status == Domain.Enums.AuditPlanStatus.Submitted)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["Status"] = new[] { "Laporan hanya dapat dibuat untuk rencana audit yang sudah disetujui." }
            });
        }

        var findings = await _db.Findings.AsNoTracking()
            .Where(f => f.AuditPlanId == request.AuditPlanId)
            .Include(f => f.Department)
            .ToListAsync(cancellationToken);

        var findingIds = findings.Select(f => f.Id).ToList();

        var caps = await _db.CorrectiveActions.AsNoTracking()
            .Where(c => findingIds.Contains(c.FindingId))
            .ToListAsync(cancellationToken);

        var capMap = caps.ToDictionary(c => c.FindingId);

        var reportFindings = findings
            .Select(f =>
            {
                var cap = capMap.TryGetValue(f.Id, out var c)
                    ? new ReportCorrectiveActionDto(
                        c.Action,
                        c.PicName,
                        c.TargetDate,
                        c.Progress,
                        c.Status.ToString())
                    : null;

                return new ReportFindingDto(
                    f.Id,
                    f.Title,
                    f.Description,
                    f.Department != null ? f.Department.Name : null,
                    f.RiskLevel.ToString(),
                    f.Category,
                    f.Recommendation,
                    f.DueDate,
                    cap);
            })
            .ToList();

        return new AuditReportDataDto(
            plan.Id,
            plan.Title,
            plan.Objective,
            plan.Scope,
            plan.Standard,
            plan.Status.ToString(),
            plan.Department != null ? plan.Department.Name : null,
            plan.StartDate,
            plan.EndDate,
            plan.Assignments
                .Select(a => new AuditReportAssignmentDto(
                    a.UserId,
                    a.User.Username,
                    a.User.FullName,
                    a.RoleInPlan))
                .ToList(),
            plan.ChecklistItems
                .Select(i => new AuditReportChecklistItemDto(
                    i.Category,
                    i.Question,
                    i.IsRequired,
                    i.Status.ToString(),
                    i.Note))
                .ToList(),
            reportFindings);
    }
}
