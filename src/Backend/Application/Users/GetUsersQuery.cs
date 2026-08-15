using IAMS.Application.Common;
using IAMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Users;

public sealed record GetUsersQuery(
    string? Search = null,
    Guid? DepartmentId = null,
    Guid? RoleId = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = Pagination.DefaultPageSize) : IRequest<PagedResult<UserDto>>;

internal sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IApplicationDbContext _db;

    public GetUsersQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(u =>
                u.Username.Contains(search)
                || u.Email.Contains(search)
                || u.FullName.Contains(search));
        }

        if (request.DepartmentId.HasValue)
            query = query.Where(u => u.DepartmentId == request.DepartmentId);

        if (request.RoleId.HasValue)
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == request.RoleId));

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive);

        var users = query
            .OrderBy(u => u.Username)
            .Select(u => new UserDto(
                u.Id,
                u.Username,
                u.Email,
                u.FullName,
                u.IsActive,
                u.MustChangePassword,
                u.DepartmentId,
                u.Department != null ? u.Department.Name : null,
                u.UserRoles.Select(ur => new RoleDto(ur.Role.Id, ur.Role.Name, ur.Role.Description)).ToList()));

        return await users.ToPagedAsync(request.Page, request.PageSize, cancellationToken);
    }
}
