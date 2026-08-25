using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Interests;
using FluentValidation;

namespace Blizka.UnitTests.UseCases.Interests;

public sealed class SearchInterestsQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА найдены интересы ТОГДА в результате имя берётся из колонки запрошенной локали")]
    public async Task Handle_maps_the_name_from_the_requested_locale()
    {
        var interest = CreateInterest("Бег", "Бег", "Running");
        var repository = new FakeInterestRepository { SearchResult = [interest] };
        var handler = new SearchInterestsQueryHandler(repository, new SearchInterestsQueryValidator());

        var result = await handler.Handle(new SearchInterestsQuery("Run", CityLocale.En), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Running", result[0].Name);
        Assert.False(result[0].IsCustom);
    }

    [Fact(DisplayName = "КОГДА q непустой ТОГДА в репозиторий передаётся обрезанный запрос и лимит 10")]
    public async Task Handle_trims_the_query_and_passes_the_limit_to_the_repository()
    {
        var repository = new FakeInterestRepository { SearchResult = [] };
        var handler = new SearchInterestsQueryHandler(repository, new SearchInterestsQueryValidator());

        await handler.Handle(new SearchInterestsQuery("  Бег  ", CityLocale.Ru), CancellationToken.None);

        Assert.Equal("Бег", repository.LastQuery);
        Assert.Equal(CityLocale.Ru, repository.LastLocale);
        Assert.Equal(10, repository.LastLimit);
    }

    [Fact(DisplayName = "КОГДА q пустой ТОГДА выбрасывается ValidationException и репозиторий не вызывается")]
    public async Task Handle_throws_ValidationException_for_an_empty_query()
    {
        var repository = new FakeInterestRepository();
        var handler = new SearchInterestsQueryHandler(repository, new SearchInterestsQueryValidator());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new SearchInterestsQuery(string.Empty, CityLocale.Ru), CancellationToken.None));
        Assert.False(repository.WasSearchCalled);
    }

    private static Interest CreateInterest(string nameRu, string nameBe, string nameEn) => new()
    {
        Id = Guid.NewGuid(),
        Category = InterestCategory.Sport,
        NameRu = nameRu,
        NameBe = nameBe,
        NameEn = nameEn,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeInterestRepository : IInterestRepository
    {
        public IReadOnlyList<Interest> SearchResult { get; set; } = [];

        public string? LastQuery { get; private set; }

        public CityLocale LastLocale { get; private set; }

        public int LastLimit { get; private set; }

        public bool WasSearchCalled { get; private set; }

        public Task<IReadOnlyList<Interest>> GetCatalogAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Interest>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken)
        {
            WasSearchCalled = true;
            LastQuery = query;
            LastLocale = locale;
            LastLimit = limit;
            return Task.FromResult(SearchResult);
        }

        public Task<IReadOnlyList<Interest>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Interest?> FindByNameAsync(string name, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddAsync(Interest interest, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
