using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Cities;
using NetTopologySuite.Geometries;

namespace Blizka.UnitTests.UseCases.Cities;

public sealed class GetCityQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА город с таким id есть в каталоге ТОГДА возвращается его имя на запрошенной локали")]
    public async Task Handle_returns_the_city_name_in_the_requested_locale()
    {
        var city = CreateCity("Минск", "Мінск", "Minsk", "BY");
        var repository = new FakeCityRepository { CityById = city };
        var handler = new GetCityQueryHandler(repository);

        var result = await handler.Handle(new GetCityQuery(city.Id, CityLocale.En), CancellationToken.None);

        Assert.Equal(city.Id, result.Id);
        Assert.Equal("Minsk", result.Name);
        Assert.Equal("BY", result.Country);
        Assert.True(result.IsOpen);
    }

    [Fact(DisplayName = "КОГДА города с таким id нет в каталоге ТОГДА выбрасывается CityNotFoundException")]
    public async Task Handle_throws_CityNotFoundException_when_the_city_does_not_exist()
    {
        var repository = new FakeCityRepository { CityById = null };
        var handler = new GetCityQueryHandler(repository);
        var cityId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<CityNotFoundException>(
            () => handler.Handle(new GetCityQuery(cityId, CityLocale.Ru), CancellationToken.None));
        Assert.Equal(cityId, exception.CityId);
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
        public City? CityById { get; set; }

        public Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<City?> GetByIdAsync(Guid cityId, CancellationToken cancellationToken) =>
            Task.FromResult(CityById);

        public Task<IReadOnlyList<City>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<City?> FindNearestAsync(Point location, double maxDistanceMeters, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
