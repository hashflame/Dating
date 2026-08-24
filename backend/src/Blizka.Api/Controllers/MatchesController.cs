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
}
