using Blizka.Api.Cities;
using Blizka.Api.Common;
using Blizka.App.UseCases.Cities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Каталог городов (T-4.1) — поиск при выборе города на онбординге и в фильтрах.</summary>
[ApiController]
[Authorize]
[Route("api/cities")]
public sealed class CitiesController(IMediator mediator) : ControllerBase
{
    /// <summary>Полнотекстовый поиск городов каталога по подстроке (pg_trgm), не более 10 результатов.</summary>
    /// <response code="200">Список найденных городов (может быть пустым).</response>
    /// <response code="400"><c>q</c> пустой или длиннее 100 символов.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("search")]
    [ProducesResponseType<ApiResponse<CityDto[]>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string? locale, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SearchCitiesQuery(q, CityLocaleParser.Parse(locale)), cancellationToken);

        return Ok(ApiResponse<CityDto[]>.Ok(result.Select(CityDto.From).ToArray()));
    }

    /// <summary>Город по id — чтобы показать название сохранённого <c>cityId</c> (например, из черновика онбординга) на клиенте.</summary>
    /// <response code="200">Город найден.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    /// <response code="404">Города с таким id нет в каталоге.</response>
    [HttpGet("{cityId:guid}")]
    [ProducesResponseType<ApiResponse<CityDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid cityId, [FromQuery] string? locale, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCityQuery(cityId, CityLocaleParser.Parse(locale)), cancellationToken);

        return Ok(ApiResponse<CityDto>.Ok(CityDto.From(result)));
    }
}
