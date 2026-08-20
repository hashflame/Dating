using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Feed;

/// <summary>По образцу <see cref="Blizka.App.UseCases.Cities.CityNameResolver"/> — выбор колонки имени интереса по локали.</summary>
internal static class InterestNameResolver
{
    public static string Resolve(Interest interest, CityLocale locale) => locale switch
    {
        CityLocale.Be => interest.NameBe,
        CityLocale.En => interest.NameEn,
        _ => interest.NameRu,
    };
}
