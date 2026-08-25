using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcureLive.Application.Notifications.Commands.MarkAllNotificationsRead;
using ProcureLive.Application.Notifications.Commands.MarkNotificationRead;
using ProcureLive.Application.Notifications.Queries.GetMyNotifications;
using ProcureLive.Application.Notifications.Queries.GetUnreadCount;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Notifications;

namespace ProcureLive.WebApi.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetMine(
        [FromQuery] bool? unreadOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        PagedResult<NotificationDto> result = await _sender.Send(new GetMyNotificationsQuery(unreadOnly, page, pageSize), cancellationToken);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount(CancellationToken cancellationToken)
    {
        int result = await _sender.Send(new GetUnreadNotificationCountQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkNotificationReadCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkAllNotificationsReadCommand(), cancellationToken);
        return NoContent();
    }
}
