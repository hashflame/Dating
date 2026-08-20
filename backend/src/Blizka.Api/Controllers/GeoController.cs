using Blizka.Api.Cities;
using Blizka.Api.Common;
using Blizka.Api.Geo;
using Blizka.App.UseCases.Cities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Blizka.Api.Controllers;

/// <summary>Определение города по геолокации устройства (T-4.1).</summary>
[ApiController]
[Authorize]
[Route("api/geo")]
public sealed class GeoController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Обратное геокодирование координат: ищет ближайший город каталога (в пределах ~50км) и дополняет
    /// ответ человекочитаемым адресом от Nominatim OSM.
    /// </summary>
    /// <response code="200">
    /// Готово. <c>city</c> — <c>null</c>, если рядом нет ни одного каталожного города (не значит ошибку —
    /// клиент в этом случае предлагает поиск городов вручную).
    /// </response>
    /// <response code="400"><c>lat</c>/<c>lon</c> вне допустимого диапазона координат.</response>
    /// <response code="401">Токен отсутствует или невалиден.</response>
    [HttpPost("detect")]
    [ProducesResponseType<ApiResponse<GeoDetectResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Detect(DetectCityRequest request, [FromQuery] string? locale, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new DetectCityQuery(request.Lat, request.Lon, CityLocaleParser.Parse(locale)),
            cancellationToken);

        var cityDto = result.City is null ? null : CityDto.From(result.City);

        return Ok(ApiResponse<GeoDetectResponse>.Ok(new GeoDetectResponse(cityDto, result.DetectedAddress)));
    }
}
