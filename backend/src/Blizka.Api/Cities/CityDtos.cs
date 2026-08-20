using Blizka.App.UseCases.Cities;

namespace Blizka.Api.Cities;

/// <summary>Город в ответах API поиска и геолокации (T-4.1).</summary>
/// <param name="Id">Id города в каталоге.</param>
/// <param name="Name">Название города на запрошенной локали.</param>
/// <param name="Country">Код страны ISO 3166-1 alpha-2 (например, <c>BY</c>).</param>
/// <param name="IsOpen">Открыт ли город для использования (MVP: всегда <c>true</c>, механика waitlist — T-4.2).</param>
public sealed record CityDto(Guid Id, string Name, string Country, bool IsOpen)
{
    public static CityDto From(CitySearchResult result) => new(result.Id, result.Name, result.Country, result.IsOpen);
}
