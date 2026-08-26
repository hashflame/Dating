using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.DatePreferences;

namespace Blizka.UnitTests.UseCases.DatePreferences;

public sealed class GetDatePreferenceCatalogQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА запрошен каталог ТОГДА предпочтения отсортированы по порядку объявления DatePreferenceCode")]
    public async Task Handle_returns_the_catalog_ordered_by_code_declaration_order()
    {
        var repository = new FakeUserDatePreferenceRepository
        {
            Catalog =
            [
                CreatePreference(DatePreferenceCode.SomethingNew),
                CreatePreference(DatePreferenceCode.ActiveOutdoors),
                CreatePreference(DatePreferenceCode.CalmHangout),
            ],
        };
        var handler = new GetDatePreferenceCatalogQueryHandler(repository);

        var result = await handler.Handle(new GetDatePreferenceCatalogQuery(CityLocale.Ru), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.Equal(
            [DatePreferenceCode.ActiveOutdoors, DatePreferenceCode.CalmHangout, DatePreferenceCode.SomethingNew],
            result.Select(r => r.Code));
    }

    [Fact(DisplayName = "КОГДА запрошена локаль ТОГДА название предпочтения берётся из соответствующей колонки")]
    public async Task Handle_maps_the_name_from_the_requested_locale()
    {
        var preference = CreatePreference(DatePreferenceCode.CalmHangout, nameEn: "Calm hangout");
        var repository = new FakeUserDatePreferenceRepository { Catalog = [preference] };
        var handler = new GetDatePreferenceCatalogQueryHandler(repository);

        var result = await handler.Handle(new GetDatePreferenceCatalogQuery(CityLocale.En), CancellationToken.None);

        Assert.Equal("Calm hangout", result[0].Name);
    }

    private static DatePreference CreatePreference(DatePreferenceCode code, string? nameEn = null) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        NameRu = code.ToString(),
        NameBe = code.ToString(),
        NameEn = nameEn ?? code.ToString(),
    };

    private sealed class FakeUserDatePreferenceRepository : IUserDatePreferenceRepository
    {
        public IReadOnlyList<DatePreference> Catalog { get; set; } = [];

        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DatePreference>> GetCatalogAsync(CancellationToken cancellationToken) => Task.FromResult(Catalog);
    }
}
