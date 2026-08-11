using IAMS.Application.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace IAMS.Api.Hubs;

/// <summary>
/// Real-time notification hub. Clients authenticate via the JWT in the query string
/// (<c>?access_token=</c>) and are placed in a per-user group named <c>u:&lt;userId&gt;</c>.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public const string Route = "/hubs/notifications";

    public static string GroupName(Guid userId) => $"u:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userId, out var id))
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(id));

        await base.OnConnectedAsync();
    }
}