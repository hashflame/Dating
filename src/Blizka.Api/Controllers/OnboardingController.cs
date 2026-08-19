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

    /// <summary>
    /// Завершает онбординг (T-2.3, S-07): проверяет, что шаги 1-3 черновика заполнены, дано согласие и
    /// загружено хотя бы одно фото, переводит пользователя в Active, переносит данные черновика в профиль
    /// и начисляет стартовые зорки.
    /// </summary>
    /// <response code="200">Онбординг завершён; возвращены начисленные зорки и заполненность профиля.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="409">Онбординг для этого пользователя уже был завершён ранее.</response>
    /// <response code="422">Не выполнено одно из условий завершения: не заполнен шаг черновика, нет согласия или нет ни одного фото.</response>
    [HttpPost("complete")]
    [ProducesResponseType<ApiResponse<OnboardingCompleteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Complete(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CompleteOnboardingCommand(User.GetUserId()), cancellationToken);

        var nextReward = result.NextReward is { } reward
            ? new NextRewardResponse(reward.Threshold, reward.SparksReward)
            : null;

        return Ok(ApiResponse<OnboardingCompleteResponse>.Ok(
            new OnboardingCompleteResponse(result.SparksAwarded, result.ProfileCompleteness, nextReward)));
    }
}
