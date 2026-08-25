using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Interests;

namespace Blizka.UnitTests.UseCases.Interests;

public sealed class GetInterestCatalogQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА запрошен каталог ТОГДА интересы группируются по категориям")]
    public async Task Handle_groups_interests_by_category()
    {
        var repository = new FakeInterestRepository
        {
            Catalog =
            [
                CreateInterest("Бег", InterestCategory.Sport),
                CreateInterest("Йога", InterestCategory.Sport),
                CreateInterest("Музыка", InterestCategory.Creativity),
            ],
        };
        var handler = new GetInterestCatalogQueryHandler(repository);

        var result = await handler.Handle(new GetInterestCatalogQuery(CityLocale.Ru), CancellationToken.None);

        Assert.Equal(2, result.Count);
        var sport = Assert.Single(result, g => g.Category == InterestCategory.Sport);
        Assert.Equal(2, sport.Interests.Count);
        var creativity = Assert.Single(result, g => g.Category == InterestCategory.Creativity);
        Assert.Single(creativity.Interests);
    }

    [Fact(DisplayName = "КОГДА запрошена локаль ТОГДА имя интереса берётся из соответствующей колонки")]
    public async Task Handle_maps_the_name_from_the_requested_locale()
    {
        var interest = CreateInterest("Бег", InterestCategory.Sport, nameEn: "Running");
        var repository = new FakeInterestRepository { Catalog = [interest] };
        var handler = new GetInterestCatalogQueryHandler(repository);

        var result = await handler.Handle(new GetInterestCatalogQuery(CityLocale.En), CancellationToken.None);

        Assert.Equal("Running", result[0].Interests[0].Name);
    }

    private static Interest CreateInterest(string nameRu, InterestCategory category, string? nameEn = null) => new()
    {
        Id = Guid.NewGuid(),
        Category = category,
        NameRu = nameRu,
        NameBe = nameRu,
        NameEn = nameEn ?? nameRu,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeInterestRepository : IInterestRepository
    {
        public IReadOnlyList<Interest> Catalog { get; set; } = [];

        public Task<IReadOnlyList<Interest>> GetCatalogAsync(CancellationToken cancellationToken) => Task.FromResult(Catalog);

        public Task<IReadOnlyList<Interest>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Interest>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Interest?> FindByNameAsync(string name, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddAsync(Interest interest, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
