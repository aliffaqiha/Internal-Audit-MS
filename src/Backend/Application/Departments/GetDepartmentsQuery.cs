using IAMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Departments;

public sealed record GetDepartmentsQuery(bool IncludeInactive = false) : IRequest<IReadOnlyList<DepartmentDto>>;

internal sealed class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IApplicationDbContext _db;

    public GetDepartmentsQueryHandler(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DepartmentDto>> Handle(
        GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Departments.AsNoTracking();

        if (!request.IncludeInactive)
            query = query.Where(d => d.IsActive);

        return await query
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.Id,
                d.Name,
                d.Description,
                d.IsActive,
                d.Users.Count(u => u.IsActive)))
            .ToListAsync(cancellationToken);
    }
}