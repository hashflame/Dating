using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Referrals;
using Blizka.App.UseCases.Referrals;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Реферальные ссылки (T-20.1, S-47) — генерация приглашения и статистика приглашённых.</summary>
[ApiController]
[Authorize]
[Route("api/referrals")]
public sealed class ReferralsController(IMediator mediator) : ControllerBase
{
    /// <summary>Генерирует (детерминированно от userId) deep link на бота с реферальным кодом и текст для шаринга.</summary>
    /// <response code="200">Ссылка и текст для приглашения.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpPost("invite")]
    [ProducesResponseType<ApiResponse<ReferralInviteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Invite(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new InviteReferralCommand(User.GetUserId(), ResolveLocale()), cancellationToken);

        return Ok(ApiResponse<ReferralInviteResponse>.Ok(ReferralInviteResponse.From(result)));
    }

    /// <summary>Статистика приглашений текущего пользователя как реферера.</summary>
    /// <response code="200">Статистика.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("stats")]
    [ProducesResponseType<ApiResponse<ReferralStatsResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Stats(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetReferralStatsQuery(User.GetUserId()), cancellationToken);

        return Ok(ApiResponse<ReferralStatsResponse>.Ok(ReferralStatsResponse.From(result)));
    }

    private string ResolveLocale() => RequestLocaleResolver.Resolve(HttpContext) switch
    {
        ApiLocale.Be => "be",
        ApiLocale.En => "en",
        _ => "ru",
    };
}
