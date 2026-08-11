using IAMS.Application.Common.Interfaces;
using IAMS.Domain.Entities;
using IAMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IAMS.Application.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _db;
    private readonly INotificationNotifier _notifier;

    public NotificationService(IApplicationDbContext db, INotificationNotifier notifier)
    {
        _db = db;
        _notifier = notifier;
    }

    public async Task SendAsync(
        IReadOnlyCollection<Guid> recipientIds,
        NotificationType type,
        string title,
        string? message = null,
        string? link = null,
        string? dedupeKey = null,
        CancellationToken cancellationToken = default)
    {
        var users = recipientIds
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (users.Count == 0)
            return;

        var created = new List<(Guid UserId, Domain.Entities.Notification Entity, NotificationDto Dto)>();

        foreach (var userId in users)
        {
            if (!string.IsNullOrWhiteSpace(dedupeKey))
            {
                var exists = await _db.Notifications
                    .AsNoTracking()
                    .AnyAsync(n => n.UserId == userId && n.DedupeKey == dedupeKey, cancellationToken);
                if (exists)
                    continue;
            }

            var entity = new Domain.Entities.Notification
            {
                UserId = userId,
                Type = type.ToString(),
                Title = title,
                Message = message,
                Link = link,
                DedupeKey = dedupeKey,
                IsRead = false
            };

            _db.Notifications.Add(entity);
            created.Add((userId, entity,
                new NotificationDto(entity.Id, entity.Type, entity.Title, entity.Message, entity.Link, entity.IsRead, entity.CreatedAt)));
        }

        if (created.Count == 0)
            return;

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var (userId, _, dto) in created)
            await _notifier.SendToUserAsync(userId, dto, cancellationToken);
    }
}