using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Feed;
using Blizka.App.UseCases.Feed;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Лента анкет (T-5.1).</summary>
[ApiController]
[Authorize]
[Route("api/feed")]
public sealed class FeedController(IMediator mediator) : ControllerBase
{
    private const int DefaultLimit = 10;

    /// <summary>
    /// Очередная порция карточек ленты: активные пользователи предпочитаемого пола из города текущего
    /// пользователя, кроме уже свайпнутых, отсортированные по убыванию совместимости.
    /// </summary>
    /// <response code="200">Карточки ленты (может быть пустым списком, см. <c>exhausted</c>).</response>
    /// <response code="400"><c>limit</c> вне диапазона 1-50.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet]
    [ProducesResponseType<ApiResponse<FeedResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFeed([FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetFeedQuery(User.GetUserId(), limit ?? DefaultLimit), cancellationToken);

        return Ok(ApiResponse<FeedResponse>.Ok(FeedResponse.From(result)));
    }
}
