using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using FluentValidation;
using MediatR;
using NetTopologySuite.Geometries;

namespace Blizka.App.UseCases.Cities;

/// <summary>
/// Обрабатывает <see cref="DetectCityQuery"/> (T-4.1). Ближайший город каталога ищется по собственным
/// координатам City (PostGIS, без завязки на написание Nominatim) — обратное геокодирование лишь
/// обогащает ответ человекочитаемым адресом и не влияет на выбор города. Оба запроса (БД и Nominatim)
/// независимы и идут параллельно, а не один за другим.
/// </summary>
public sealed class DetectCityQueryHandler(
    ICityRepository cityRepository,
    INominatimGeocoder geocoder,
    IValidator<DetectCityQuery> validator)
    : IRequestHandler<DetectCityQuery, GeoDetectResult>
{
    // Между соседними каталожными городами Беларуси десятки километров — 50км с запасом покрывает "ближайший
    // разумный" город и не подставляет случайный далёкий город тому, кто оказался в глуши между ними.
    private const double MaxDistanceMeters = 50_000;

    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    public async Task<GeoDetectResult> Handle(DetectCityQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var point = GeometryFactory.CreatePoint(new Coordinate(request.Lon, request.Lat));
        var nearestCityTask = cityRepository.FindNearestAsync(point, MaxDistanceMeters, cancellationToken);
        var detectedAddressTask = ReverseGeocodeSafelyAsync(request, cancellationToken);

        await Task.WhenAll(nearestCityTask, detectedAddressTask);

        var nearestCity = nearestCityTask.Result;
        var cityResult = nearestCity is null
            ? null
            : new CitySearchResult(
                nearestCity.Id,
                CityNameResolver.Resolve(nearestCity, request.Locale),
                nearestCity.Country,
                nearestCity.IsOpen,
                nearestCity.Region,
                nearestCity.Type);

        return new GeoDetectResult(cityResult, detectedAddressTask.Result);
    }

    private async Task<string?> ReverseGeocodeSafelyAsync(DetectCityQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await geocoder.ReverseGeocodeAsync(request.Lat, request.Lon, request.Locale, cancellationToken);
        }
        // Nominatim — вспомогательное обогащение ответа, а не критичный путь: подбор города каталога (выше)
        // от него не зависит. Сеть/таймаут/лимитер могут бросить что угодно (HttpRequestException,
        // TaskCanceledException от HttpClient.Timeout, JsonException и т.д.) — фильтр по
        // cancellationToken.IsCancellationRequested, а не по типу исключения, отличает "Nominatim подвис
        // или упал" (гасим, отдаём null) от настоящей отмены самого запроса (даём распространиться дальше).
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
