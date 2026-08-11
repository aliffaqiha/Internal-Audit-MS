using IAMS.Application.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IAMS.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<NotificationDto>>> Get(
        [FromQuery] int take = 50, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetMyNotificationsQuery(take), ct));

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> UnreadCount(CancellationToken ct = default)
        => Ok(await _sender.Send(new GetUnreadCountQuery(), ct));

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct = default)
    {
        await _sender.Send(new MarkNotificationReadCommand(id), ct);
        return NoContent();
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
    {
        await _sender.Send(new MarkAllNotificationsReadCommand(), ct);
        return NoContent();
    }
}