using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Blizka.App.UseCases.Cities;
using FluentValidation;
using NetTopologySuite.Geometries;

namespace Blizka.UnitTests.UseCases.Cities;

public sealed class DetectCityQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА рядом есть город каталога ТОГДА он возвращается вместе с адресом от геокодера")]
    public async Task Handle_returns_the_nearest_city_and_the_geocoded_address()
    {
        var city = CreateCity("Минск", "Мінск", "Minsk", "BY");
        var repository = new FakeCityRepository { NearestResult = city };
        var geocoder = new FakeNominatimGeocoder("Мінск, Беларусь");
        var handler = new DetectCityQueryHandler(repository, geocoder, new DetectCityQueryValidator());

        var result = await handler.Handle(new DetectCityQuery(53.9, 27.55, CityLocale.Ru), CancellationToken.None);

        Assert.NotNull(result.City);
        Assert.Equal("Минск", result.City!.Name);
        Assert.Equal("Мінск, Беларусь", result.DetectedAddress);
    }

    [Fact(DisplayName = "КОГДА рядом нет города каталога ТОГДА City == null, а адрес от геокодера всё равно возвращается")]
    public async Task Handle_returns_a_null_city_when_nothing_is_within_range()
    {
        var repository = new FakeCityRepository { NearestResult = null };
        var geocoder = new FakeNominatimGeocoder("Somewhere");
        var handler = new DetectCityQueryHandler(repository, geocoder, new DetectCityQueryValidator());

        var result = await handler.Handle(new DetectCityQuery(0, 0, CityLocale.Ru), CancellationToken.None);

        Assert.Null(result.City);
        Assert.Equal("Somewhere", result.DetectedAddress);
    }

    [Fact(DisplayName = "КОГДА геокодер падает с исключением ТОГДА оно не прерывает обработку, а адрес возвращается null")]
    public async Task Handle_swallows_geocoder_failures()
    {
        var city = CreateCity("Минск", "Мінск", "Minsk", "BY");
        var repository = new FakeCityRepository { NearestResult = city };
        var geocoder = new FakeNominatimGeocoder(exception: new HttpRequestException("Nominatim is down"));
        var handler = new DetectCityQueryHandler(repository, geocoder, new DetectCityQueryValidator());

        var result = await handler.Handle(new DetectCityQuery(53.9, 27.55, CityLocale.Ru), CancellationToken.None);

        Assert.NotNull(result.City);
        Assert.Null(result.DetectedAddress);
    }

    [Fact(DisplayName = "КОГДА широта вне диапазона [-90, 90] ТОГДА выбрасывается ValidationException и репозиторий не вызывается")]
    public async Task Handle_throws_ValidationException_for_an_out_of_range_latitude()
    {
        var repository = new FakeCityRepository();
        var handler = new DetectCityQueryHandler(repository, new FakeNominatimGeocoder(null), new DetectCityQueryValidator());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new DetectCityQuery(999, 27.55, CityLocale.Ru), CancellationToken.None));
        Assert.False(repository.WasFindNearestCalled);
    }

    [Fact(DisplayName = "КОГДА долгота вне диапазона [-180, 180] ТОГДА выбрасывается ValidationException и репозиторий не вызывается")]
    public async Task Handle_throws_ValidationException_for_an_out_of_range_longitude()
    {
        var repository = new FakeCityRepository();
        var handler = new DetectCityQueryHandler(repository, new FakeNominatimGeocoder(null), new DetectCityQueryValidator());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new DetectCityQuery(53.9, 999, CityLocale.Ru), CancellationToken.None));
        Assert.False(repository.WasFindNearestCalled);
    }

    [Fact(DisplayName = "КОГДА запрошена локаль En ТОГДА геокодер вызывается с этой же локалью (accept-language)")]
    public async Task Handle_passes_the_requested_locale_to_the_geocoder()
    {
        var repository = new FakeCityRepository();
        var geocoder = new FakeNominatimGeocoder(null);
        var handler = new DetectCityQueryHandler(repository, geocoder, new DetectCityQueryValidator());

        await handler.Handle(new DetectCityQuery(53.9, 27.55, CityLocale.En), CancellationToken.None);

        Assert.Equal(CityLocale.En, geocoder.LastLocale);
    }

    private static City CreateCity(string nameRu, string nameBe, string nameEn, string country) => new()
    {
        Id = Guid.NewGuid(),
        NameRu = nameRu,
        NameBe = nameBe,
        NameEn = nameEn,
        Country = country,
        Coordinates = new Point(0, 0),
        IsOpen = true,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeCityRepository : ICityRepository
    {
        public City? NearestResult { get; set; }

        public bool WasFindNearestCalled { get; private set; }

        public Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<City>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<City?> FindNearestAsync(Point location, double maxDistanceMeters, CancellationToken cancellationToken)
        {
            WasFindNearestCalled = true;
            return Task.FromResult(NearestResult);
        }
    }

    private sealed class FakeNominatimGeocoder(string? address = null, Exception? exception = null) : INominatimGeocoder
    {
        public CityLocale LastLocale { get; private set; }

        public Task<string?> ReverseGeocodeAsync(double lat, double lon, CityLocale locale, CancellationToken cancellationToken)
        {
            LastLocale = locale;
            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(address);
        }
    }
}
