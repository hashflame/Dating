using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Onboarding;
using Blizka.App.UseCases.Onboarding;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Черновик анкеты онбординга (T-2.1) — пошаговое сохранение данных регистрации с возможностью продолжить с того же места.</summary>
[ApiController]
[Authorize]
[Route("api/onboarding")]
public sealed class OnboardingController(IMediator mediator) : ControllerBase
{
    /// <summary>Сохраняет данные одного шага онбординга, перезаписывая ранее сохранённые данные этого же шага.</summary>
    /// <response code="200">Данные шага сохранены; возвращён обновлённый черновик.</response>
    /// <response code="400">Данные шага не прошли валидацию.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpPatch("draft")]
    [ProducesResponseType<ApiResponse<OnboardingDraftResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PatchDraft(PatchOnboardingDraftRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new PatchOnboardingDraftCommand(User.GetUserId(), request.Step, request.Data),
            cancellationToken);

        return Ok(ApiResponse<OnboardingDraftResponse>.Ok(new OnboardingDraftResponse(result.Step, result.Data)));
    }

    /// <summary>Возвращает текущий шаг и все сохранённые данные черновика онбординга текущего пользователя.</summary>
    /// <response code="200">Черновик найден (или пользователь ещё не начинал онбординг — возвращается пустое состояние).</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("draft")]
    [ProducesResponseType<ApiResponse<OnboardingDraftResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDraft(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOnboardingDraftQuery(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<OnboardingDraftResponse>.Ok(new OnboardingDraftResponse(result.Step, result.Data)));
    }
}
