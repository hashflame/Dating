namespace Blizka.App.UseCases.Cities;

/// <param name="City">
/// Ближайший город каталога в пределах разумного радиуса, либо <c>null</c>, если рядом нет ни одного каталожного города.
/// </param>
/// <param name="DetectedAddress">
/// Человекочитаемый адрес от обратного геокодирования (Nominatim) — для отображения клиенту, когда
/// <paramref name="City"/> не найден, либо когда сам сервис геокодирования недоступен.
/// </param>
public sealed record GeoDetectResult(CitySearchResult? City, string? DetectedAddress);
