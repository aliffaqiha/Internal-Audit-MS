using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Findings;

public sealed record GetFindingByIdQuery(Guid Id) : IRequest<FindingDto>;

internal sealed class GetFindingByIdQueryHandler : IRequestHandler<GetFindingByIdQuery, FindingDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetFindingByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<FindingDto> Handle(GetFindingByIdQuery request, CancellationToken cancellationToken)
    {
        var scope = await CurrentUserAccess.ResolveAsync(_db, _currentUser, cancellationToken);

        var finding = await _db.Findings
            .AsNoTracking()
            .Include(f => f.Evidences)
            .Where(f => f.Id == request.Id)
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
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Temuan tidak ditemukan.");

        CurrentUserAccess.EnsureCanAccessFinding(scope, finding.DepartmentId);

        return finding;
    }
}
