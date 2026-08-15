using IAMS.Application.Common;
using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Audits;

public sealed record GetAuditPlansQuery(
    AuditPlanStatus? Status = null,
    Guid? DepartmentId = null,
    int Page = 1,
    int PageSize = Pagination.DefaultPageSize) : IRequest<PagedResult<AuditPlanDto>>;

internal sealed class GetAuditPlansQueryHandler : IRequestHandler<GetAuditPlansQuery, PagedResult<AuditPlanDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAuditPlansQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<AuditPlanDto>> Handle(
        GetAuditPlansQuery request, CancellationToken cancellationToken)
    {
        var scope = await CurrentUserAccess.ResolveAsync(_db, _currentUser, cancellationToken);

        var query = _db.AuditPlans.AsNoTracking().RestrictPlans(scope);

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status);
        if (request.DepartmentId.HasValue)
            query = query.Where(p => p.DepartmentId == request.DepartmentId);

        var plans = query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new AuditPlanDto(
                p.Id,
                p.Title,
                p.Objective,
                p.Scope,
                p.Standard,
                p.StartDate,
                p.EndDate,
                p.Status,
                p.DepartmentId,
                p.Department != null ? p.Department.Name : null,
                p.Assignments.Select(a => new AuditPlanAssignmentDto(
                    a.UserId,
                    a.User!.Username,
                    a.User!.FullName,
                    a.RoleInPlan)).ToList(),
                p.ChecklistItems.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id)
                    .Select(i => new AuditPlanChecklistItemDto(
                        i.Id, i.Question, i.Category, i.IsRequired, i.Status, i.Note)).ToList()));

        return await plans.ToPagedAsync(request.Page, request.PageSize, cancellationToken);
    }
}
