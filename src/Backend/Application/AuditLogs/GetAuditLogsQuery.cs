using IAMS.Application.Common;
using IAMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.AuditLogs;

public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserName,
    string Action,
    string Entity,
    string? EntityId,
    string? IpAddress,
    string? OldValues,
    string? NewValues,
    DateTimeOffset CreatedAt);

public sealed record GetAuditLogsQuery(
    string? Search = null,
    string? Entity = null,
    int Page = 1,
    int PageSize = Pagination.DefaultPageSize) : IRequest<PagedResult<AuditLogDto>>;

internal sealed class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAuditLogsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Entity))
            query = query.Where(a => a.Entity == request.Entity);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToUpperInvariant();
            query = query.Where(a =>
                (a.UserName != null && a.UserName.ToUpper().Contains(term))
                || a.Action.ToUpper().Contains(term)
                || (a.EntityId != null && a.EntityId.ToUpper().Contains(term)));
        }

        var logs = query
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new AuditLogDto(
                a.Id,
                a.UserId,
                a.UserName,
                a.Action,
                a.Entity,
                a.EntityId,
                a.IpAddress,
                a.OldValues,
                a.NewValues,
                a.CreatedAt));

        return await logs.ToPagedAsync(request.Page, request.PageSize, cancellationToken);
    }
}
