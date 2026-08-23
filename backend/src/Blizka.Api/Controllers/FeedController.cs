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

/// <summary>Лента анкет, свайпы и фильтры (T-5.1, T-5.2, T-5.3, T-5.4).</summary>
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
    /// <response code="429">Дневной лимит свайпов (spec 002, B3) исчерпан.</response>
    [HttpPost("{userId:guid}/like")]
    [ProducesResponseType<ApiResponse<SwipeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> Like(Guid userId, CancellationToken cancellationToken) =>
        Swipe(userId, SwipeType.Like, cancellationToken);

    /// <summary>Дизлайк.</summary>
    /// <response code="200">Свайп принят.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Пользователь <paramref name="userId"/> не найден.</response>
    /// <response code="409">Этот пользователь уже свайпнут и не отменён (T-5.3).</response>
    /// <response code="429">Дневной лимит свайпов (spec 002, B3) исчерпан.</response>
    [HttpPost("{userId:guid}/dislike")]
    [ProducesResponseType<ApiResponse<SwipeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> Dislike(Guid userId, CancellationToken cancellationToken) =>
        Swipe(userId, SwipeType.Dislike, cancellationToken);

    /// <summary>Суперлайк — списывает зорки (стоимость в конфиге <c>Sparks:SuperlikeCost</c>), при взаимном лайке создаёт мэтч.</summary>
    /// <response code="200">Свайп принят.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="402">Недостаточно зорок.</response>
    /// <response code="404">Пользователь <paramref name="userId"/> не найден.</response>
    /// <response code="409">Этот пользователь уже свайпнут и не отменён (T-5.3).</response>
    /// <response code="429">Дневной лимит свайпов (spec 002, B3) исчерпан.</response>
    [HttpPost("{userId:guid}/superlike")]
    [ProducesResponseType<ApiResponse<SwipeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status402PaymentRequired)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> Superlike(Guid userId, CancellationToken cancellationToken) =>
        Swipe(userId, SwipeType.Superlike, cancellationToken);

    private async Task<IActionResult> Swipe(Guid userId, SwipeType type, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SwipeCommand(User.GetUserId(), userId, type), cancellationToken);

        return Ok(ApiResponse<SwipeResponse>.Ok(SwipeResponse.From(result)));
    }

    /// <summary>
    /// Отменяет последний свайп (S-10, notes) — не более 3 раз за скользящие 24 часа. Если отменённый лайк
    /// привёл к мэтчу, а контакт по нему ещё не открыт — мэтч удаляется; за отменённый суперлайк зорки возвращаются.
    /// </summary>
    /// <response code="200">Свайп отменён — <c>undosRemaining</c> показывает, сколько отмен ещё доступно.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="409">Отменять нечего — нет активного свайпа.</response>
    /// <response code="422">Дневной лимит отмен (3) исчерпан.</response>
    [HttpPost("undo")]
    [ProducesResponseType<ApiResponse<UndoSwipeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Undo(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UndoSwipeCommand(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<UndoSwipeResponse>.Ok(UndoSwipeResponse.From(result)));
    }

    /// <summary>
    /// Текущие фильтры ленты (S-15). Если пользователь их ещё не сохранял (например, онбординг был пройден
    /// до появления этой задачи), возвращаются MVP-дефолты, а не ошибка.
    /// </summary>
    /// <response code="200">Фильтры (сохранённые либо дефолтные).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("filters")]
    [ProducesResponseType<ApiResponse<FeedFiltersResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFilters(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetFeedFiltersQuery(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<FeedFiltersResponse>.Ok(FeedFiltersResponse.From(result)));
    }

    /// <summary>
    /// Частично обновляет фильтры ленты (S-15) — присланные поля перезаписывают сохранённые, остальные не
    /// трогаются. При первом сохранении недостающие поля берут MVP-дефолты. <c>activeWithinDays: -1</c>
    /// выключает фильтр активности (обычный <c>null</c> здесь означает "не трогать", а не "выключить").
    /// </summary>
    /// <response code="200">Фильтры обновлены; возвращено полное текущее состояние.</response>
    /// <response code="400">Данные не прошли валидацию (например, AgeRange.Min &gt;= AgeRange.Max).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpPatch("filters")]
    [ProducesResponseType<ApiResponse<FeedFiltersResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PatchFilters(PatchFeedFiltersRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request.ToCommand(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<FeedFiltersResponse>.Ok(FeedFiltersResponse.From(result)));
    }
}
