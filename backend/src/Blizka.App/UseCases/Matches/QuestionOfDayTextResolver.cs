using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Matches;

/// <summary>Выбор локализованного текста вопроса дня (T-11.1) — по образцу <see cref="Cities.CityNameResolver"/>.</summary>
internal static class QuestionOfDayTextResolver
{
    public static string Resolve(QuestionOfDay question, CityLocale locale) => locale switch
    {
        CityLocale.Be => question.TextBe,
        CityLocale.En => question.TextEn,
        _ => question.TextRu,
    };
}
