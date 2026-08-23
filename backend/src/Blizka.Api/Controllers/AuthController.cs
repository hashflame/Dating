using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.App.Telegram;
using Blizka.App.UseCases.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>Обменивает Telegram WebApp initData на сессионный JWT.</summary>
    /// <remarks>
    /// Требует сырую строку Telegram WebApp initData в заголовке запроса <c>X-Telegram-InitData</c>
    /// (проверяется <see cref="Blizka.Api.Auth.TelegramAuthMiddleware"/> до выполнения этого action'а -
    /// тела запроса нет). При первом входе создаёт пользователя (<see cref="AuthTelegramResponse.IsNewUser"/>).
    /// </remarks>
    /// <response code="200">Аутентификация прошла успешно; возвращён bearer-токен.</response>
    /// <response code="401">Заголовок <c>X-Telegram-InitData</c> отсутствует или невалиден.</response>
    [HttpPost("telegram")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<AuthTelegramResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Telegram(CancellationToken cancellationToken)
    {
        if (HttpContext.Items[TelegramAuthMiddleware.ItemsKey] is not TelegramInitData initData)
        {
            var locale = RequestLocaleResolver.Resolve(HttpContext);
            var message = ErrorMessageCatalog.Resolve(ErrorMessageCatalog.TelegramInitDataInvalid, locale);
            return Unauthorized(ApiErrorResponse.From(ErrorMessageCatalog.TelegramInitDataInvalid, message));
        }

        var result = await mediator.Send(new AuthenticateTelegramUserCommand(initData), cancellationToken);

        var response = new AuthTelegramResponse(
            result.Token,
            result.ExpiresAt,
            result.UserId,
            result.Status,
            result.IsNewUser,
            result.Locale);

        return Ok(ApiResponse<AuthTelegramResponse>.Ok(response));
    }
}
