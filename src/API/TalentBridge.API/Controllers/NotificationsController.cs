using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TalentBridge.Shared.Interfaces;
using TalentBridge.Notifications.Application.Commands.MarkAllRead;
using TalentBridge.Notifications.Application.Commands.MarkRead;
using TalentBridge.Notifications.Application.Queries.GetNotifications;

namespace TalentBridge.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(CancellationToken ct)
    {
        var result = await mediator.Send(new GetNotificationsQuery(currentUser.UserId), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await mediator.Send(new MarkNotificationReadCommand(id, currentUser.UserId), ct);
        return NoContent();
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await mediator.Send(new MarkAllNotificationsReadCommand(currentUser.UserId), ct);
        return NoContent();
    }
}
