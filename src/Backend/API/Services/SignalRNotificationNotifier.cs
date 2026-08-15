using IAMS.Application.Common.Interfaces;
using IAMS.Application.Notifications;
using IAMS.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace IAMS.Api.Services;

/// <summary>Pushes persisted notifications to connected users via SignalR.</summary>
public sealed class SignalRNotificationNotifier : INotificationNotifier
{
    private readonly IHubContext<NotificationHub> _hub;

    public SignalRNotificationNotifier(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task SendToUserAsync(Guid userId, NotificationPushedDto notification, CancellationToken cancellationToken = default)
        => _hub.Clients.Group(NotificationHub.GroupName(userId))
            .SendAsync("NotificationReceived", notification, cancellationToken);

    public Task SendToUsersAsync(IReadOnlyCollection<Guid> userIds, NotificationPushedDto notification, CancellationToken cancellationToken = default)
    {
        var groups = userIds.Distinct().Select(NotificationHub.GroupName);
        return _hub.Clients.Groups(groups).SendAsync("NotificationReceived", notification, cancellationToken);
    }
}