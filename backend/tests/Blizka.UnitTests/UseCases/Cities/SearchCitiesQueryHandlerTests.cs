using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Cities;
using FluentValidation;
using NetTopologySuite.Geometries;

namespace Blizka.UnitTests.UseCases.Cities;

public sealed class SearchCitiesQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА найдены города ТОГДА в результате имя берётся из колонки запрошенной локали")]
    public async Task Handle_maps_the_name_from_the_requested_locale()
    {
        var city = CreateCity("Минск", "Мінск", "Minsk", "BY");
        var repository = new FakeCityRepository { SearchResult = [city] };
        var handler = new SearchCitiesQueryHandler(repository, new SearchCitiesQueryValidator());

        var result = await handler.Handle(new SearchCitiesQuery("Vil", CityLocale.En), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Minsk", result[0].Name);
        Assert.Equal("BY", result[0].Country);
        Assert.True(result[0].IsOpen);
    }

    [Fact(DisplayName = "КОГДА q непустой ТОГДА в репозиторий передаётся обрезанный запрос и лимит 10")]
    public async Task Handle_trims_the_query_and_passes_the_limit_to_the_repository()
    {
        var repository = new FakeCityRepository { SearchResult = [] };
        var handler = new SearchCitiesQueryHandler(repository, new SearchCitiesQueryValidator());

        await handler.Handle(new SearchCitiesQuery("  Минск  ", CityLocale.Ru), CancellationToken.None);

        Assert.Equal("Минск", repository.LastQuery);
        Assert.Equal(CityLocale.Ru, repository.LastLocale);
        Assert.Equal(10, repository.LastLimit);
    }

    [Fact(DisplayName = "КОГДА q пустой ТОГДА выбрасывается ValidationException и репозиторий не вызывается")]
    public async Task Handle_throws_ValidationException_for_an_empty_query()
    {
        var repository = new FakeCityRepository();
        var handler = new SearchCitiesQueryHandler(repository, new SearchCitiesQueryValidator());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new SearchCitiesQuery(string.Empty, CityLocale.Ru), CancellationToken.None));
        Assert.False(repository.WasSearchCalled);
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
        public IReadOnlyList<City> SearchResult { get; set; } = [];

        public string? LastQuery { get; private set; }

        public CityLocale LastLocale { get; private set; }

        public int LastLimit { get; private set; }

        public bool WasSearchCalled { get; private set; }

        public Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<City>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken)
        {
            WasSearchCalled = true;
            LastQuery = query;
            LastLocale = locale;
            LastLimit = limit;
            return Task.FromResult(SearchResult);
        }

        public Task<City?> FindNearestAsync(Point location, double maxDistanceMeters, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
