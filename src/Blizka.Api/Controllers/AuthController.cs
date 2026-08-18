using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.App.Telegram;
using Blizka.App.UseCases.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("telegram")]
    [AllowAnonymous]
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
            result.IsNewUser);

        return Ok(ApiResponse<AuthTelegramResponse>.Ok(response));
    }
}
