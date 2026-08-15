using IAMS.Application.Common;
using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Findings;

public sealed record GetFindingsQuery(
    RiskLevel? RiskLevel = null,
    Guid? DepartmentId = null,
    Guid? AuditPlanId = null,
    string? Search = null,
    int Page = 1,
    int PageSize = Pagination.DefaultPageSize) : IRequest<PagedResult<FindingDto>>;

internal sealed class GetFindingsQueryHandler : IRequestHandler<GetFindingsQuery, PagedResult<FindingDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetFindingsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<FindingDto>> Handle(GetFindingsQuery request, CancellationToken cancellationToken)
    {
        var scope = await CurrentUserAccess.ResolveAsync(_db, _currentUser, cancellationToken);

        var query = _db.Findings
            .Include(f => f.Evidences)
            .AsNoTracking()
            .RestrictFindings(scope);

        if (request.RiskLevel.HasValue)
            query = query.Where(f => f.RiskLevel == request.RiskLevel);

        if (request.DepartmentId.HasValue)
            query = query.Where(f => f.DepartmentId == request.DepartmentId);

        if (request.AuditPlanId.HasValue)
            query = query.Where(f => f.AuditPlanId == request.AuditPlanId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(f => EF.Functions
                .ToTsVector("simple",
                    f.Title + " " + (f.Category ?? "") + " " + (f.Description ?? ""))
                .Matches(EF.Functions.WebSearchToTsQuery("simple", search)));
        }

        var findings = query
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
                f.AuditPlan != null ? f.AuditPlan.Title : null,
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
                    .ToList()));

        return await findings.ToPagedAsync(request.Page, request.PageSize, cancellationToken);
    }
}
