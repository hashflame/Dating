using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Ideas;
using Blizka.App.UseCases.Ideas;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Доска идей (T-19.1, S-60) — community-предложения с голосованием.</summary>
[ApiController]
[Authorize]
[Route("api/ideas")]
public sealed class IdeasController(IMediator mediator) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;
    private const string DefaultTab = "new";

    /// <summary>
    /// Список идей (T-19.1). <c>tab</c>: <c>hot</c> (по голосам), <c>new</c> (по свежести, по умолчанию),
    /// <c>inWork</c> (статусы underReview+planned), <c>mine</c> (идеи текущего пользователя).
    /// </summary>
    /// <response code="200">Страница идей.</response>
    /// <response code="400"><c>tab</c> не из допустимых значений, либо <c>page</c>/<c>pageSize</c> вне диапазона.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet]
    [ProducesResponseType<ApiResponse<PaginatedResponse<IdeaDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetIdeas(
        [FromQuery] string? tab, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetIdeasQuery(User.GetUserId(), tab ?? DefaultTab, page ?? DefaultPage, pageSize ?? DefaultPageSize),
            cancellationToken);

        var response = new PaginatedResponse<IdeaDto>(
            result.Items.Select(IdeaDto.From).ToArray(), result.Page, result.PageSize, result.TotalCount);

        return Ok(ApiResponse<PaginatedResponse<IdeaDto>>.Ok(response));
    }

    /// <summary>
    /// Отправить идею на доску (T-19.1). Бонус <c>Sparks:IdeaSubmissionBonusAmount</c> начисляется не чаще
    /// раза в календарный месяц — см. <see cref="CreateIdeaResponse.SparksAwarded"/>.
    /// </summary>
    /// <response code="200">Идея создана.</response>
    /// <response code="400">Пустой текст либо он длиннее 500 символов.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpPost]
    [ProducesResponseType<ApiResponse<CreateIdeaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateIdea([FromBody] CreateIdeaRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateIdeaCommand(User.GetUserId(), request.Text, request.Anonymous), cancellationToken);

        return Ok(ApiResponse<CreateIdeaResponse>.Ok(CreateIdeaResponse.From(result)));
    }

    /// <summary>
    /// Проголосовать за идею (T-19.1). Идемпотентно: повторный тап того же пользователя ничего не меняет —
    /// повторное голосование за ту же идею отклоняется на уровне хранения (один голос на пару идея/пользователь).
    /// </summary>
    /// <response code="204">Голос учтён (или уже был учтён раньше).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Идеи с таким id нет.</response>
    [HttpPost("{ideaId:guid}/vote")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Vote(Guid ideaId, CancellationToken cancellationToken)
    {
        await mediator.Send(new VoteOnIdeaCommand(User.GetUserId(), ideaId), cancellationToken);

        return NoContent();
    }

    /// <summary>Снять голос с идеи (T-19.1). Идемпотентно — если голоса не было, тоже возвращает 204.</summary>
    /// <response code="204">Голос снят (или его не было).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpDelete("{ideaId:guid}/vote")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveVote(Guid ideaId, CancellationToken cancellationToken)
    {
        await mediator.Send(new RemoveIdeaVoteCommand(User.GetUserId(), ideaId), cancellationToken);

        return NoContent();
    }
}
