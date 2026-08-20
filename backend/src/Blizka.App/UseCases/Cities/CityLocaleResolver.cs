using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Cities;

/// <summary>
/// Общий разбор строки локали (primary subtag до дефиса, регистронезависимо) в <see cref="CityLocale"/> —
/// единая точка для Api-слоя (<see cref="Blizka.Api.Cities.CityLocaleParser"/>, query-параметр запроса) и
/// App-слоя (например, сохранённая <c>User.Locale</c> в ленте, T-5.1) вместо дублирования одного switch
/// в обоих местах. Публичный, а не <c>internal</c>, как <see cref="CityNameResolver"/>, ровно поэтому —
/// Api-слою нужен доступ через границу сборки.
/// </summary>
public static class CityLocaleResolver
{
    public static CityLocale Resolve(string? value)
    {
        var primarySubtag = value?.Trim().ToLowerInvariant().Split('-')[0];

        return primarySubtag switch
        {
            "be" => CityLocale.Be,
            "en" => CityLocale.En,
            _ => CityLocale.Ru,
        };
    }
}
