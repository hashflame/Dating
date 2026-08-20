using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Cities;

internal static class CityNameResolver
{
    public static string Resolve(City city, CityLocale locale) => locale switch
    {
        CityLocale.Be => city.NameBe,
        CityLocale.En => city.NameEn,
        _ => city.NameRu,
    };
}
