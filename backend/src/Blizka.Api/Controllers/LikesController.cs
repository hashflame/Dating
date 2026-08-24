using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Likes;
using Blizka.App.UseCases.Likes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Входящие и исходящие лайки (T-6.1, S-21).</summary>
[ApiController]
[Authorize]
[Route("api/likes")]
public sealed class LikesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Кто лайкнул текущего пользователя, без уже смэтченных (S-21, «Вам нравятся»). До разблокировки — только
    /// <c>count</c> и заблюренное превью главных фото; после — полный список.
    /// </summary>
    /// <response code="200">Список (заблюренный либо полный, в зависимости от <c>revealed</c>).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("incoming")]
    [ProducesResponseType<ApiResponse<IncomingLikesResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetIncoming(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetIncomingLikesQuery(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<IncomingLikesResponse>.Ok(IncomingLikesResponse.From(result)));
    }

    /// <summary>Кого лайкнул текущий пользователь, без уже смэтченных (S-21, «Вы нравитесь»).</summary>
    /// <response code="200">Полный список — этот список разблокировки не требует.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("outgoing")]
    [ProducesResponseType<ApiResponse<OutgoingLikesResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetOutgoing(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOutgoingLikesQuery(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<OutgoingLikesResponse>.Ok(OutgoingLikesResponse.From(result)));
    }

    /// <summary>
    /// Списывает зорки (стоимость — <c>Sparks:LikesRevealCost</c>) и разблокирует список входящих лайков
    /// навсегда, не за каждого лайкнувшего отдельно. Повторный вызов после разблокировки идемпотентен —
    /// зорки повторно не списываются.
    /// </summary>
    /// <response code="200">Список разблокирован (или уже был) — <c>sparksSpent</c> и полный список.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="402">Недостаточно зорок.</response>
    /// <response code="409">Конкурентный запрос на разблокировку — повторите попытку.</response>
    [HttpPost("incoming/reveal")]
    [ProducesResponseType<ApiResponse<RevealIncomingLikesResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reveal(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RevealIncomingLikesCommand(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<RevealIncomingLikesResponse>.Ok(RevealIncomingLikesResponse.From(result)));
    }
}
