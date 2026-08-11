using IAMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Notifications;

public sealed record GetUnreadCountQuery : IRequest<int>;

internal sealed class GetUnreadCountQueryHandler : IRequestHandler<GetUnreadCountQuery, int>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadCountQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return 0;

        return await _db.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId.Value && !n.IsRead, cancellationToken);
    }
}