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
    private const int DefaultArchivePage = 1;
    private const int DefaultArchivePageSize = 20;

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

    /// <summary>
    /// Ручная архивация мэтча (T-7.4, S-30 notes) — доступна в любом состоянии (new/waitingForMessage), не только
    /// протухшего. Идемпотентна: повторный вызов на уже заархивированном мэтче ничего не меняет.
    /// </summary>
    /// <response code="204">Мэтч заархивирован (или уже был заархивирован ранее — идемпотентно).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Мэтча с таким id нет — в том числе если он есть, но текущий пользователь не его участник.</response>
    [HttpPost("{matchId:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveMatch(Guid matchId, CancellationToken cancellationToken)
    {
        await mediator.Send(new ArchiveMatchCommand(matchId, User.GetUserId()), cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Возврат мэтча из архива (T-7.4, S-30 notes) — бесплатно, без ограничения по числу вызовов. Идемпотентна: на
    /// уже активном мэтче ничего не меняет. Если мэтч всё ещё подпадает под условие автоархивации, следующий
    /// прогон джобы <c>ArchiveStaleMatches</c> (до 6 часов) заархивирует его снова.
    /// </summary>
    /// <response code="204">Мэтч возвращён из архива (или уже был активен — идемпотентно).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Мэтча с таким id нет — в том числе если он есть, но текущий пользователь не его участник.</response>
    [HttpDelete("{matchId:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnarchiveMatch(Guid matchId, CancellationToken cancellationToken)
    {
        await mediator.Send(new UnarchiveMatchCommand(matchId, User.GetUserId()), cancellationToken);

        return NoContent();
    }

    /// <summary>Вопрос дня (T-11.1, S-37) — текущий вопрос, мой ответ и ответ партнёра (виден только когда ответили оба).</summary>
    /// <response code="200">Текущий вопрос дня и ответы.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Мэтча с таким id нет — в том числе если он есть, но текущий пользователь не его участник.</response>
    [HttpGet("{matchId:guid}/question-of-day")]
    [ProducesResponseType<ApiResponse<QuestionOfDayResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestionOfDay(Guid matchId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetQuestionOfDayQuery(matchId, User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<QuestionOfDayResponse>.Ok(QuestionOfDayResponse.From(result)));
    }

    /// <summary>
    /// Ответ на вопрос дня (T-11.1, S-37). Идемпотентно: повторный вызов не перезаписывает уже сохранённый
    /// ответ. Когда ответили оба участника, обоим уходит Telegram-уведомление.
    /// </summary>
    /// <response code="200">Мой сохранённый ответ (новый или уже существовавший).</response>
    /// <response code="400">Пустой текст ответа или он длиннее 1000 символов.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Мэтча с таким id нет — в том числе если он есть, но текущий пользователь не его участник.</response>
    /// <response code="409">Вопрос дня ещё не опубликован, либо сохранение ответа столкнулось с параллельным запросом — повторите.</response>
    [HttpPost("{matchId:guid}/question-of-day/answer")]
    [ProducesResponseType<ApiResponse<QuestionAnswerDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AnswerQuestionOfDay(
        Guid matchId, [FromBody] AnswerQuestionOfDayRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new AnswerQuestionOfDayCommand(matchId, User.GetUserId(), request.Text), cancellationToken);

        return Ok(ApiResponse<QuestionAnswerDto>.Ok(QuestionAnswerDto.From(result)!));
    }

    /// <summary>Архив вопросов дня (T-11.1, S-37) — прошлые вопросы, на которые этот мэтч уже отвечал, новые сверху.</summary>
    /// <response code="200">Страница архива.</response>
    /// <response code="400"><c>page</c> меньше 1 либо <c>pageSize</c> вне диапазона 1-50.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Мэтча с таким id нет — в том числе если он есть, но текущий пользователь не его участник.</response>
    [HttpGet("{matchId:guid}/questions/archive")]
    [ProducesResponseType<ApiResponse<PaginatedResponse<QuestionArchiveItemDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuestionArchive(
        Guid matchId, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetQuestionArchiveQuery(matchId, User.GetUserId(), page ?? DefaultArchivePage, pageSize ?? DefaultArchivePageSize),
            cancellationToken);

        var response = new PaginatedResponse<QuestionArchiveItemDto>(
            result.Items.Select(QuestionArchiveItemDto.From).ToArray(), result.Page, result.PageSize, result.TotalCount);

        return Ok(ApiResponse<PaginatedResponse<QuestionArchiveItemDto>>.Ok(response));
    }

    /// <summary>
    /// Идеи свидания (T-12.1, S-39) — MVP-заглушка: подбор из фиксированного каталога по пересечению
    /// предпочтений на свидания обоих участников (T-9.3), без реальной LLM-генерации (T-13.1 ещё не реализована).
    /// </summary>
    /// <response code="200">От 0 до 3 идей свидания.</response>
    /// <response code="400"><c>maxBudget</c> не положительный либо <c>currency</c> не из 3 символов.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Мэтча с таким id нет — в том числе если он есть, но текущий пользователь не его участник.</response>
    [HttpGet("{matchId:guid}/date-ideas")]
    [ProducesResponseType<ApiResponse<DateIdeasResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDateIdeas(
        Guid matchId, [FromQuery] string? city, [FromQuery] decimal? maxBudget, [FromQuery] string? currency, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetDateIdeasQuery(matchId, User.GetUserId(), city, maxBudget, currency), cancellationToken);

        return Ok(ApiResponse<DateIdeasResponse>.Ok(DateIdeasResponse.From(result)));
    }

    /// <summary>
    /// Подтверждение договорённости о встрече (T-12.1, S-39). Идемпотентно: повторный вызов не сдвигает
    /// <c>DateConfirmedAt</c>. Фоновая джоба пост-опроса через 24 часа (decomposition.md T-12.1) не реализована.
    /// </summary>
    /// <response code="204">Договорённость зафиксирована (или уже была зафиксирована ранее — идемпотентно).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Мэтча с таким id нет — в том числе если он есть, но текущий пользователь не его участник.</response>
    [HttpPost("{matchId:guid}/date-confirmed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmDate(Guid matchId, CancellationToken cancellationToken)
    {
        await mediator.Send(new ConfirmDateCommand(matchId, User.GetUserId()), cancellationToken);

        return NoContent();
    }
}
