using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.DatePreferences;

/// <summary>По образцу <see cref="Feed.InterestNameResolver"/> — выбор колонки имени предпочтения по локали.</summary>
internal static class DatePreferenceNameResolver
{
    public static string Resolve(DatePreference preference, CityLocale locale) => locale switch
    {
        CityLocale.Be => preference.NameBe,
        CityLocale.En => preference.NameEn,
        _ => preference.NameRu,
    };
}
