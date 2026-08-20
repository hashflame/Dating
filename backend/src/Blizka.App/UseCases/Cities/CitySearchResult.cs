namespace Blizka.App.UseCases.Cities;

/// <summary>Город в результатах поиска/геолокации (T-4.1).</summary>
public sealed record CitySearchResult(Guid Id, string Name, string Country, bool IsOpen);
