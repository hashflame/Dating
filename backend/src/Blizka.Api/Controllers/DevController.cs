using System.Security.Cryptography;
using System.Text;
using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Dev;
using Blizka.Api.ErrorHandling;
using Blizka.App.UseCases.Dev;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Blizka.Api.Controllers;

/// <summary>
/// Dev-инструменты для ручного тестирования фронтенда на prod (спека 003, docs/specs/003-demo-seed-data.md).
/// Не привязан к JWT — доступ регулируется общим секретом <c>DevLogin:Secret</c> (тот же, что у dev-логина
/// в <see cref="TelegramAuthMiddleware"/>), пустым по умолчанию: без явно заданной переменной на сервере
/// эндпоинт всегда отвечает 401.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/dev")]
public sealed class DevController(IMediator mediator, IConfiguration configuration) : ControllerBase
{
    /// <summary>Сносит и заново создаёт 10 фиксированных демо-пользователей вместе с их фото/интересами/мэтчами/лайками.</summary>
    /// <response code="200">Список демо-аккаунтов (telegramId для заголовка X-Dev-Login-TelegramId).</response>
    /// <response code="401"><c>DevLogin:Secret</c> не задан на сервере, либо переданный секрет неверен/отсутствует.</response>
    [HttpPost("reseed-demo-data")]
    [ProducesResponseType<ApiResponse<ReseedDemoDataResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReseedDemoData(CancellationToken cancellationToken)
    {
        var configuredSecret = configuration["DevLogin:Secret"];
        var providedSecret = Request.Headers[TelegramAuthMiddleware.DevLoginSecretHeaderName].ToString();

        if (string.IsNullOrEmpty(configuredSecret) || string.IsNullOrEmpty(providedSecret)
            || !FixedTimeEquals(configuredSecret, providedSecret))
        {
            var locale = RequestLocaleResolver.Resolve(HttpContext);
            var message = ErrorMessageCatalog.Resolve(ErrorMessageCatalog.DevAccessDenied, locale);
            return Unauthorized(ApiErrorResponse.From(ErrorMessageCatalog.DevAccessDenied, message));
        }

        var result = await mediator.Send(new ReseedDemoDataCommand(), cancellationToken);

        return Ok(ApiResponse<ReseedDemoDataResponse>.Ok(ReseedDemoDataResponse.From(result)));
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        return bytesA.Length == bytesB.Length && CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
