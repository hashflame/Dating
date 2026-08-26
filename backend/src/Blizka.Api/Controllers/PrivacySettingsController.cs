using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Privacy;
using Blizka.App.UseCases.Privacy;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Настройки приватности текущего пользователя (T-16.1, S-51).</summary>
[ApiController]
[Authorize]
[Route("api/privacy/settings")]
public sealed class PrivacySettingsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Текущие настройки приватности (T-16.1). Если пользователь ни разу не открывал экран, строки в БД ещё
    /// нет — это не 404, а значения по умолчанию (всё выключено, кроме <c>showLastActive</c>).
    /// </summary>
    /// <response code="200">Настройки приватности.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet]
    [ProducesResponseType<ApiResponse<PrivacySettingsResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPrivacySettingsQuery(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<PrivacySettingsResponse>.Ok(PrivacySettingsResponse.From(result)));
    }

    /// <summary>
    /// Частично обновляет настройки приватности (T-16.1): <c>blockIncomingMessages</c> — «пишет первой сама»
    /// в хабе мэтча; <c>hideDistance</c>/<c>hideAge</c> — скрывают поля в ленте; <c>showLastActive</c> — «был(а)
    /// недавно»; <c>invisibleMode</c> — только для подписчиков «Безлимит» (T-8.3), включение без подписки — 422.
    /// Не переданное (<c>null</c>) поле не меняется.
    /// </summary>
    /// <response code="200">Настройки обновлены.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="422">Попытка включить <c>invisibleMode</c> без активной подписки «Безлимит».</response>
    [HttpPatch]
    [ProducesResponseType<ApiResponse<PrivacySettingsResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> PatchSettings(PatchPrivacySettingsRequest request, CancellationToken cancellationToken)
    {
        var command = new PatchPrivacySettingsCommand(
            User.GetUserId(),
            request.BlockIncomingMessages,
            request.HideDistance,
            request.HideAge,
            request.ShowLastActive,
            request.InvisibleMode);

        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponse<PrivacySettingsResponse>.Ok(PrivacySettingsResponse.From(result)));
    }
}
