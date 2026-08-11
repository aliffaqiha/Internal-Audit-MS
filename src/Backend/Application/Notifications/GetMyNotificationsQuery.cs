using IAMS.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Notifications;

public sealed record GetMyNotificationsQuery(int Take = 50) : IRequest<IReadOnlyList<NotificationDto>>;

internal sealed class GetMyNotificationsQueryHandler : IRequestHandler<GetMyNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public GetMyNotificationsQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<NotificationDto>> Handle(
        GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId;
        if (!userId.HasValue)
            return Array.Empty<NotificationDto>();

        var take = Math.Clamp(request.Take, 1, 200);

        return await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.Link,
                n.IsRead,
                n.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}