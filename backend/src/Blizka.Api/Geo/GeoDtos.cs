using Blizka.Api.Cities;

namespace Blizka.Api.Geo;

/// <summary>Тело запроса <c>POST /api/geo/detect</c>.</summary>
/// <param name="Lat">Широта в градусах (WGS84).</param>
/// <param name="Lon">Долгота в градусах (WGS84).</param>
public sealed record DetectCityRequest(double Lat, double Lon);

/// <summary>Ответ <c>POST /api/geo/detect</c>.</summary>
/// <param name="City">
/// Ближайший город каталога в пределах разумного радиуса, либо <c>null</c>, если рядом нет ни одного каталожного города.
/// </param>
/// <param name="DetectedAddress">
/// Человекочитаемый адрес от обратного геокодирования — для отображения клиенту, когда <paramref name="City"/>
/// не найден, либо <c>null</c>, если геокодирование не удалось.
/// </param>
public sealed record GeoDetectResponse(CityDto? City, string? DetectedAddress);
