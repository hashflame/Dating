using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Services;

/// <summary>
/// Обратное геокодирование координат в человекочитаемый адрес — обёртка над Nominatim OSM
/// (T-4.1, <c>POST /api/geo/detect</c>). Используется только как вспомогательная подпись места для клиента,
/// когда рядом нет ни одного города каталога — сам подбор ближайшего каталожного города идёт через
/// <see cref="Repositories.ICityRepository.FindNearestAsync"/> по собственным координатам City, без завязки
/// на то, как Nominatim называет населённый пункт (написание/транслитерация могут не совпадать с сидингом).
/// </summary>
public interface INominatimGeocoder
{
    /// <summary>
    /// Возвращает <c>display_name</c> из ответа Nominatim на языке <paramref name="locale"/>, либо <c>null</c>,
    /// если сервис недоступен/не вернул результат/исчерпан локальный rate limit.
    /// </summary>
    Task<string?> ReverseGeocodeAsync(double lat, double lon, CityLocale locale, CancellationToken cancellationToken);
}
