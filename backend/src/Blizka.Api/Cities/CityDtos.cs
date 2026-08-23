using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Cities;

namespace Blizka.Api.Cities;

/// <summary>Город в ответах API поиска и геолокации (T-4.1).</summary>
/// <param name="Id">Id города в каталоге.</param>
/// <param name="Name">Название города на запрошенной локали.</param>
/// <param name="Country">Код страны ISO 3166-1 alpha-2 (например, <c>BY</c>).</param>
/// <param name="IsOpen">Открыт ли город для использования (MVP: всегда <c>true</c>, механика waitlist — T-4.2).</param>
/// <param name="Region">Область (для BY) или страна (для диаспоры), если задана (spec 002, B11).</param>
/// <param name="Type">Гранулярность населённого пункта — город или посёлок (spec 002, B11).</param>
public sealed record CityDto(Guid Id, string Name, string Country, bool IsOpen, string? Region, CityType Type)
{
    public static CityDto From(CitySearchResult result) =>
        new(result.Id, result.Name, result.Country, result.IsOpen, result.Region, result.Type);
}
