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

    /// <summary>Гасит бейдж(и) непрочитанного — вызывается при открытии соответствующего таба (список симпатий / мэтчи).</summary>
    /// <response code="204">Бейдж(и) погашены.</response>
    /// <response code="400">Ни <c>likes</c>, ни <c>matches</c> не выставлены в <c>true</c>.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="409">Параллельный запрос, мутирующий этого же пользователя, сохранился первым — повторите запрос.</response>
    [HttpPost("seen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MarkSeen([FromBody] MarkNotificationsSeenRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(new MarkNotificationsSeenCommand(User.GetUserId(), request.Likes, request.Matches), cancellationToken);

        return NoContent();
    }
}
