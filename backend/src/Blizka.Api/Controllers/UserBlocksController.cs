using Blizka.Api.Auth;
using Blizka.Api.Blocks;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.App.UseCases.Blocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Блокировка других пользователей текущим пользователем (T-16.2, S-51).</summary>
[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UserBlocksController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Блокирует пользователя (T-16.2): он перестаёт появляться в ленте текущего пользователя и наоборот,
    /// а свайп в любую сторону между парой становится недоступен. Идемпотентно — повторный вызов на уже
    /// заблокированном пользователе тоже возвращает 204.
    /// </summary>
    /// <response code="204">Пользователь заблокирован.</response>
    /// <response code="400">Попытка заблокировать самого себя.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Пользователь не найден.</response>
    [HttpPost("{userId:guid}/block")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Block(Guid userId, CancellationToken cancellationToken)
    {
        await mediator.Send(new BlockUserCommand(User.GetUserId(), userId), cancellationToken);

        return NoContent();
    }

    /// <summary>Снимает блокировку с пользователя (T-16.2). Идемпотентно — если блокировки не было, тоже возвращает 204.</summary>
    /// <response code="204">Блокировка снята.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpDelete("{userId:guid}/block")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Unblock(Guid userId, CancellationToken cancellationToken)
    {
        await mediator.Send(new UnblockUserCommand(User.GetUserId(), userId), cancellationToken);

        return NoContent();
    }

    /// <summary>Список заблокированных текущим пользователем (T-16.2), от самой свежей блокировки к старым.</summary>
    /// <response code="200">Список заблокированных (может быть пустым).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("me/blocked")]
    [ProducesResponseType<ApiResponse<BlockedUserResponse[]>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBlocked(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetBlockedUsersQuery(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<BlockedUserResponse[]>.Ok(result.Select(BlockedUserResponse.From).ToArray()));
    }
}
