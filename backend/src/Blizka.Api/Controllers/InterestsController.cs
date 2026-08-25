using Blizka.Api.Cities;
using Blizka.Api.Common;
using Blizka.Api.Interests;
using Blizka.App.UseCases.Interests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Каталог интересов (T-9.2) — выбор при редактировании профиля.</summary>
[ApiController]
[Authorize]
[Route("api/interests")]
public sealed class InterestsController(IMediator mediator) : ControllerBase
{
    /// <summary>Полный каталог интересов, сгруппированный по категориям.</summary>
    /// <response code="200">Каталог интересов.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("catalog")]
    [ProducesResponseType<ApiResponse<InterestCategoryDto[]>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCatalog([FromQuery] string? locale, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetInterestCatalogQuery(CityLocaleParser.Parse(locale)), cancellationToken);

        return Ok(ApiResponse<InterestCategoryDto[]>.Ok(result.Select(InterestCategoryDto.From).ToArray()));
    }

    /// <summary>Полнотекстовый поиск по каталогу интересов по подстроке (pg_trgm), не более 10 результатов.</summary>
    /// <response code="200">Список найденных интересов (может быть пустым).</response>
    /// <response code="400"><c>q</c> пустой или длиннее 50 символов.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpGet("search")]
    [ProducesResponseType<ApiResponse<InterestDto[]>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] string? locale, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SearchInterestsQuery(q, CityLocaleParser.Parse(locale)), cancellationToken);

        return Ok(ApiResponse<InterestDto[]>.Ok(result.Select(InterestDto.From).ToArray()));
    }
}
