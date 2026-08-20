using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Cities;

namespace Blizka.Api.Cities;

/// <summary>
/// Разбирает query-параметр <c>locale</c> для <c>GET /api/cities/search</c> и <c>POST /api/geo/detect</c> (T-4.1)
/// в <see cref="CityLocale"/>. По формату повторяет <see cref="Blizka.Api.ErrorHandling.ApiLocaleParser"/>,
/// но живёт отдельно: та локаль — для сообщений об ошибках API-слоя, эта — для выбора колонки имени города
/// в App/Data-слоях, и переиспользовать один enum между ними противоречило бы направлению зависимостей.
/// Сам разбор строки делегирован в <see cref="CityLocaleResolver"/> (App-слой) — там же он нужен и вне
/// Api-слоя (T-5.1, сохранённая локаль профиля), дублировать один switch в обоих слоях не стали.
/// </summary>
public static class CityLocaleParser
{
    public const CityLocale Default = CityLocale.Ru;

    public static CityLocale Parse(string? value) => CityLocaleResolver.Resolve(value);
}
