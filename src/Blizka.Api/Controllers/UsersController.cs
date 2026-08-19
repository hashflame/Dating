using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Consent;
using Blizka.App.UseCases.Consent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Действия текущего пользователя над собственным профилем.</summary>
[ApiController]
[Authorize]
[Route("api/users/me")]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Фиксирует юридическое согласие пользователя (T-2.2) с временной меткой, IP-адресом и Telegram id —
    /// на фронте кнопка недоступна без чекбокса, но бэкенд тоже проверяет это при завершении онбординга (defense in depth).
    /// </summary>
    /// <response code="200">Согласие зафиксировано.</response>
    /// <response code="400">Тело запроса не прошло валидацию.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpPost("consent")]
    [ProducesResponseType<ApiResponse<UserConsentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RecordConsent(RecordConsentRequest request, CancellationToken cancellationToken)
    {
        var command = new RecordUserConsentCommand(
            User.GetUserId(),
            User.GetTelegramId(),
            request.Type,
            request.Version,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<UserConsentResponse>.Ok(new UserConsentResponse(result.Type, result.Version, result.Timestamp)));
    }
}
