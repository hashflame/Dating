using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Notifications;
using Blizka.App.UseCases.Notifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Уведомления (T-10.2).</summary>
[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    /// <summary>Счётчик непрочитанного для бейджа — активные входящие лайки и новые (ещё без открытого контакта) мэтчи.</summary>
    /// <response code="200">Счётчик.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("unread")]
    [ProducesResponseType<ApiResponse<UnreadNotificationsResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnread(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUnreadNotificationsCountQuery(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<UnreadNotificationsResponse>.Ok(UnreadNotificationsResponse.From(result)));
    }
}
