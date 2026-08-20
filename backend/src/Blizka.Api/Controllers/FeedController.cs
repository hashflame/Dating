using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Feed;
using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Feed;
using Blizka.App.UseCases.Swipes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Лента анкет и свайпы (T-5.1, T-5.2).</summary>
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

    /// <summary>Лайк. При взаимном лайке создаёт мэтч (S-16) и возвращает три входа для начала общения.</summary>
    /// <response code="200">Свайп принят — <c>isMatch</c> и <c>match</c> сообщают, создан ли мэтч.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Пользователь <paramref name="userId"/> не найден.</response>
    /// <response code="409">Этот пользователь уже свайпнут и не отменён (T-5.3).</response>
    [HttpPost("{userId:guid}/like")]
    [ProducesResponseType<ApiResponse<SwipeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Like(Guid userId, CancellationToken cancellationToken) =>
        Swipe(userId, SwipeType.Like, cancellationToken);

    /// <summary>Дизлайк.</summary>
    /// <response code="200">Свайп принят.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Пользователь <paramref name="userId"/> не найден.</response>
    /// <response code="409">Этот пользователь уже свайпнут и не отменён (T-5.3).</response>
    [HttpPost("{userId:guid}/dislike")]
    [ProducesResponseType<ApiResponse<SwipeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Dislike(Guid userId, CancellationToken cancellationToken) =>
        Swipe(userId, SwipeType.Dislike, cancellationToken);

    /// <summary>Суперлайк — списывает зорки (стоимость в конфиге <c>Sparks:SuperlikeCost</c>), при взаимном лайке создаёт мэтч.</summary>
    /// <response code="200">Свайп принят.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="402">Недостаточно зорок.</response>
    /// <response code="404">Пользователь <paramref name="userId"/> не найден.</response>
    /// <response code="409">Этот пользователь уже свайпнут и не отменён (T-5.3).</response>
    [HttpPost("{userId:guid}/superlike")]
    [ProducesResponseType<ApiResponse<SwipeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Superlike(Guid userId, CancellationToken cancellationToken) =>
        Swipe(userId, SwipeType.Superlike, cancellationToken);

    private async Task<IActionResult> Swipe(Guid userId, SwipeType type, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SwipeCommand(User.GetUserId(), userId, type), cancellationToken);

        return Ok(ApiResponse<SwipeResponse>.Ok(SwipeResponse.From(result)));
    }
}
