using Blizka.Api.Cities;
using Blizka.Api.Common;
using Blizka.Api.DatePreferences;
using Blizka.App.UseCases.DatePreferences;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Каталог предпочтений по формату свидания (T-9.3) — выбор при редактировании профиля.</summary>
[ApiController]
[Authorize]
[Route("api/date-preferences")]
public sealed class DatePreferencesController(IMediator mediator) : ControllerBase
{
    /// <summary>Полный каталог предпочтений по формату свидания (4 фиксированных значения).</summary>
    /// <response code="200">Каталог предпочтений.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("catalog")]
    [ProducesResponseType<ApiResponse<DatePreferenceDto[]>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCatalog([FromQuery] string? locale, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDatePreferenceCatalogQuery(CityLocaleParser.Parse(locale)), cancellationToken);

        return Ok(ApiResponse<DatePreferenceDto[]>.Ok(result.Select(DatePreferenceDto.From).ToArray()));
    }
}
