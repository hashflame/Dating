using System.Security.Cryptography;
using System.Text;
using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Dev;
using Blizka.Api.ErrorHandling;
using Blizka.App.UseCases.Dev;
using Blizka.App.UseCases.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Blizka.Api.Controllers;

/// <summary>
/// Dev-инструменты для ручного тестирования фронтенда на prod (спека 003, docs/specs/003-demo-seed-data.md).
/// Большинство методов не привязаны к JWT — доступ регулируется общим секретом <c>DevLogin:Secret</c> (тот же,
/// что у dev-логина в <see cref="TelegramAuthMiddleware"/>), пустым по умолчанию: без явно заданной переменной
/// на сервере такой эндпоинт всегда отвечает 401. Исключение — <see cref="ResetMyState"/>: действует только
/// над самим вызывающим (как <c>/api/feed/undo</c>), поэтому защищён обычным <c>[Authorize]</c> вместо секрета.
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

    /// <summary>
    /// Приводит текущего аутентифицированного пользователя в состояние "как сразу после онбординга":
    /// чистит его лайки/мэтчи/фото/интересы/предпочтения, обнуляет пороговые бонусы и баланс зорок
    /// (тикет ClickUp 869epwyw2). Заменяет собой прежний хак dev panel с <c>POST /api/feed/undo</c>
    /// (тот отменяет только последний свайп, а не полное состояние). В отличие от остальных методов этого
    /// контроллера — не <c>AllowAnonymous</c>+секрет, а обычный JWT текущего пользователя, как
    /// <c>/api/feed/undo</c>: действие всегда над собой, секрет тут не даёт дополнительной защиты.
    /// </summary>
    /// <response code="204">Состояние сброшено.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="409">Параллельный запрос, мутирующий этого же пользователя, сохранился первым — повторите запрос.</response>
    [Authorize]
    [HttpPost("reset-my-state")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResetMyState(CancellationToken cancellationToken)
    {
        await mediator.Send(new ResetUserStateCommand(User.GetUserId()), cancellationToken);

        return NoContent();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a);
        var bytesB = Encoding.UTF8.GetBytes(b);
        return bytesA.Length == bytesB.Length && CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
