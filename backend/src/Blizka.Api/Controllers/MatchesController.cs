using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Matches;
using Blizka.App.UseCases.Matches;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Список мэтчей (T-7.1, S-30).</summary>
[ApiController]
[Authorize]
[Route("api/matches")]
public sealed class MatchesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Три секции: <c>new</c> — контакт ещё не открыт, <c>waitingForMessage</c> — контакт открыт, но нет
    /// подтверждения отправки сообщения, <c>archived</c> — заархивированные.
    /// </summary>
    /// <response code="200">Списки мэтчей по трём секциям.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet]
    [ProducesResponseType<ApiResponse<MatchesResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMatches(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMatchesQuery(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<MatchesResponse>.Ok(MatchesResponse.From(result)));
    }

    /// <summary>Хаб мэтча (T-7.2, S-31) — детальная карточка со статусами всех веток общения.</summary>
    /// <response code="200">Карточка мэтча.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Мэтча с таким id нет — в том числе если он есть, но текущий пользователь не его участник.</response>
    [HttpGet("{matchId:guid}")]
    [ProducesResponseType<ApiResponse<MatchHubResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMatchHub(Guid matchId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetMatchHubQuery(matchId, User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<MatchHubResponse>.Ok(MatchHubResponse.From(result)));
    }

    /// <summary>
    /// Открытие контакта за зорки (T-7.3, S-32/S-36) — списывает <c>Sparks:ContactUnlockCost</c> и открывает
    /// <c>telegramUsername</c>/<c>deepLink</c> навсегда для этого мэтча. Идемпотентно: повторный вызов уже
    /// открытого контакта (в том числе вторым участником мэтча) не списывает зорки повторно.
    /// </summary>
    /// <response code="200">Контакт открыт (или уже был открыт ранее) — telegramUsername и deepLink.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="402">Недостаточно зорок.</response>
    /// <response code="404">Мэтча с таким id нет — в том числе если он есть, но текущий пользователь не его участник.</response>
    /// <response code="409">Открытие контакта столкнулось с параллельным изменением баланса — повторите запрос.</response>
    [HttpPost("{matchId:guid}/unlock")]
    [ProducesResponseType<ApiResponse<UnlockContactResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UnlockContact(Guid matchId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UnlockContactCommand(matchId, User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<UnlockContactResponse>.Ok(UnlockContactResponse.From(result)));
    }

    /// <summary>
    /// Метрика «получилось написать?» (T-7.3, S-36) — фронт вызывает после возврата из Telegram deep link'а.
    /// Не отправляет сообщение и не влияет на бизнес-логику, кроме таймера архивации T-7.4.
    /// </summary>
    /// <response code="204">Отметка сохранена (или уже была сохранена ранее — идемпотентно).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Мэтча с таким id нет — в том числе если он есть, но текущий пользователь не его участник.</response>
    [HttpPost("{matchId:guid}/message-sent-check")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MessageSentCheck(Guid matchId, CancellationToken cancellationToken)
    {
        await mediator.Send(new MessageSentCheckCommand(matchId, User.GetUserId()), cancellationToken);

        return NoContent();
    }
}
