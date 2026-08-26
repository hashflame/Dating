using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Reports;
using Blizka.App.UseCases.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Жалобы на других пользователей (T-17.1, S-13).</summary>
[ApiController]
[Authorize]
[Route("api/users")]
public sealed class ReportsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Подаёт жалобу на пользователя. Критичные причины (<c>underage</c>, <c>unsafe_meeting</c>) блокируют
    /// аккаунт немедленно, до ручной проверки модератором. При <c>blockUser: true</c> дополнительно ставится
    /// блокировка, как при <c>POST /api/users/{userId}/block</c> (T-16.2).
    /// </summary>
    /// <response code="204">Жалоба принята.</response>
    /// <response code="400">Некорректные данные — например, попытка пожаловаться на самого себя.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Пользователь не найден.</response>
    [HttpPost("{userId:guid}/report")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Report(Guid userId, CreateReportRequest request, CancellationToken cancellationToken)
    {
        await mediator.Send(
            new CreateReportCommand(User.GetUserId(), userId, request.Reason, request.Comment, request.BlockUser),
            cancellationToken);

        return NoContent();
    }
}
