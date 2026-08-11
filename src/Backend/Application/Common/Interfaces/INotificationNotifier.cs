using IAMS.Application.Notifications;

namespace IAMS.Application.Common.Interfaces;

/// <summary>Pushes a persisted notification to connected SignalR clients in real time.</summary>
/// <remarks>Backed by <c>NotificationHub</c>; a no-op when nobody is connected.</remarks>
public interface INotificationNotifier
{
    Task SendToUserAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default);

    Task SendToUsersAsync(IReadOnlyCollection<Guid> userIds, NotificationDto notification, CancellationToken cancellationToken = default);
}