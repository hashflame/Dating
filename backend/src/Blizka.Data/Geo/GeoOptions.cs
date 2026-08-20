namespace Blizka.Data.Geo;

/// <summary>Настройки обратного геокодирования (T-4.1) — секция <c>Geo</c> в appsettings.yaml.</summary>
public sealed class GeoOptions
{
    public const string SectionName = "Geo";

    /// <summary>Базовый URL Nominatim API (публичный инстанс OSM или самостоятельно захостенный аналог).</summary>
    public string NominatimBaseUrl { get; set; } = "https://nominatim.openstreetmap.org";

    /// <summary>
    /// Значение заголовка User-Agent — обязательно по usage policy публичного Nominatim
    /// (https://operations.osmfoundation.org/policies/nominatim/), должно однозначно идентифицировать приложение.
    /// </summary>
    public string NominatimUserAgent { get; set; } = string.Empty;
}
