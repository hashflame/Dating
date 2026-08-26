using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.DatePreferences;

namespace Blizka.Api.DatePreferences;

/// <summary>Предпочтение по формату свидания в ответах API (T-9.3).</summary>
/// <param name="Id">Id предпочтения.</param>
/// <param name="Code">Код предпочтения (стабильный, не зависит от локали).</param>
/// <param name="Name">Название на запрошенной локали.</param>
public sealed record DatePreferenceDto(Guid Id, DatePreferenceCode Code, string Name)
{
    public static DatePreferenceDto From(DatePreferenceCatalogItemResult result) => new(result.Id, result.Code, result.Name);
}
