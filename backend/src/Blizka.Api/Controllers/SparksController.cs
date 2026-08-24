using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.Sparks;
using Blizka.App.UseCases.Sparks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Кошелёк зорок (T-8.1, S-46/S-07).</summary>
[ApiController]
[Authorize]
[Route("api/sparks")]
public sealed class SparksController(IMediator mediator) : ControllerBase
{
    private const int DefaultPage = 1;
    private const int DefaultPageSize = 20;

    /// <summary>Баланс, страница истории операций (новые сверху) и каталог способов заработать зорки.</summary>
    /// <response code="200">Кошелёк.</response>
    /// <response code="400"><c>page</c> меньше 1 либо <c>pageSize</c> вне диапазона 1-50.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("wallet")]
    [ProducesResponseType<ApiResponse<SparksWalletResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetWallet(
        [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new GetSparksWalletQuery(User.GetUserId(), page ?? DefaultPage, pageSize ?? DefaultPageSize),
            cancellationToken);

        return Ok(ApiResponse<SparksWalletResponse>.Ok(SparksWalletResponse.From(result)));
    }
}
