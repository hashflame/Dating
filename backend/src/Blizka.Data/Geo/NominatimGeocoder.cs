using System.Globalization;
using System.Text.Json;
using System.Threading.RateLimiting;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Services;

namespace Blizka.Data.Geo;

/// <summary>Реализация <see cref="INominatimGeocoder"/> поверх HTTP API Nominatim (T-4.1).</summary>
public sealed class NominatimGeocoder(HttpClient httpClient, RateLimiter rateLimiter) : INominatimGeocoder
{
    public async Task<string?> ReverseGeocodeAsync(double lat, double lon, CityLocale locale, CancellationToken cancellationToken)
    {
        // Публичный Nominatim держит лимит 1 запрос/сек с одного IP (usage policy) — лимитер общий на все
        // одновременные запросы к /api/geo/detect, а не на один. Если очередь уже заполнена (см. регистрацию
        // лимитера в DataServiceCollectionExtensions), просто пропускаем обогащение вместо накопления бэклога
        // или риска забанить по IP весь бэкенд разом.
        using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);
        if (!lease.IsAcquired)
        {
            return null;
        }

        var language = AcceptLanguage(locale);
        var url = string.Create(CultureInfo.InvariantCulture,
            $"reverse?format=jsonv2&lat={lat}&lon={lon}&zoom=10&accept-language={language}");

        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        return document.RootElement.TryGetProperty("display_name", out var displayName)
            ? displayName.GetString()
            : null;
    }

    private static string AcceptLanguage(CityLocale locale) => locale switch
    {
        CityLocale.Be => "be",
        CityLocale.En => "en",
        _ => "ru",
    };
}
