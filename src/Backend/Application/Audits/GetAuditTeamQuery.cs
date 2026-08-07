using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Audits;

public sealed record AuditTeamMemberDto(Guid UserId, string Username, string FullName);

public sealed record GetAuditTeamQuery : IRequest<IReadOnlyList<AuditTeamMemberDto>>;

internal sealed class GetAuditTeamQueryHandler : IRequestHandler<GetAuditTeamQuery, IReadOnlyList<AuditTeamMemberDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAuditTeamQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AuditTeamMemberDto>> Handle(
        GetAuditTeamQuery request, CancellationToken cancellationToken)
    {
        var teamRoleNames = new[]
        {
            RoleConstants.Auditor,
            RoleConstants.Manager,
            RoleConstants.Administrator
        };
        var normalized = teamRoleNames.Select(RoleConstants.Normalize).ToList();

        return await _db.Users.AsNoTracking()
            .Where(u => u.IsActive)
            .Where(u => u.UserRoles.Any(ur => normalized.Contains(ur.Role.NormalizedName)))
            .OrderBy(u => u.FullName)
            .Select(u => new AuditTeamMemberDto(u.Id, u.Username, u.FullName))
            .ToListAsync(cancellationToken);
    }
}