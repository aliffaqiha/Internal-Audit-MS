using IAMS.Application.Common.DataScoping;
using IAMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Audits;

public sealed record GetAuditPlanByIdQuery(Guid Id) : IRequest<AuditPlanDto>;

internal sealed class GetAuditPlanByIdQueryHandler : IRequestHandler<GetAuditPlanByIdQuery, AuditPlanDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetAuditPlanByIdQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AuditPlanDto> Handle(GetAuditPlanByIdQuery request, CancellationToken cancellationToken)
    {
        var scope = await CurrentUserAccess.ResolveAsync(_db, _currentUser, cancellationToken);

        var plan = await _db.AuditPlans.AsNoTracking()
            .Where(p => p.Id == request.Id)
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
                        i.Id, i.Question, i.Category, i.IsRequired, i.Status, i.Note)).ToList()))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Rencana audit tidak ditemukan.");

        CurrentUserAccess.EnsureCanAccessPlan(scope, plan.DepartmentId);

        return plan;
    }
}