using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Findings;

public sealed record GetFindingsQuery(
    RiskLevel? RiskLevel = null,
    Guid? DepartmentId = null,
    Guid? AuditPlanId = null,
    string? Search = null) : IRequest<IReadOnlyList<FindingDto>>;

internal sealed class GetFindingsQueryHandler : IRequestHandler<GetFindingsQuery, IReadOnlyList<FindingDto>>
{
    private readonly IApplicationDbContext _db;

    public GetFindingsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<FindingDto>> Handle(GetFindingsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Findings
            .Include(f => f.Evidences)
            .AsNoTracking();

        if (request.RiskLevel.HasValue)
            query = query.Where(f => f.RiskLevel == request.RiskLevel);

        if (request.DepartmentId.HasValue)
            query = query.Where(f => f.DepartmentId == request.DepartmentId);

        if (request.AuditPlanId.HasValue)
            query = query.Where(f => f.AuditPlanId == request.AuditPlanId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(f =>
                f.Title.ToUpper().Contains(term.ToUpper())
                || (f.Category != null && f.Category.ToUpper().Contains(term.ToUpper()))
                || (f.Description != null && f.Description.ToUpper().Contains(term.ToUpper())));
        }

        var findings = await query
            .OrderByDescending(f => f.RiskLevel)
            .ThenByDescending(f => f.CreatedAt)
            .Select(f => new FindingDto(
                f.Id,
                f.Title,
                f.Description,
                f.DepartmentId,
                f.Department != null ? f.Department.Name : null,
                f.RiskLevel,
                f.Category,
                f.Recommendation,
                f.DueDate,
                f.AuditPlanId,
                f.Evidences
                    .OrderByDescending(e => e.Version)
                    .Select(e => new FindingEvidenceDto(
                        e.Id,
                        e.OriginalFileName,
                        e.ContentType,
                        e.SizeBytes,
                        e.Version,
                        e.CreatedAt,
                        e.CreatedBy))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return findings;
    }
}