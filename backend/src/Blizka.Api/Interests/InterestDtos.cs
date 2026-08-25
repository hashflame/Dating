using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Interests;

namespace Blizka.Api.Interests;

/// <summary>Интерес каталога в ответах API (T-9.2).</summary>
/// <param name="Id">Id интереса.</param>
/// <param name="Name">Название на запрошенной локали.</param>
/// <param name="IsCustom">Создан пользователем, а не входит в стартовый каталог.</param>
public sealed record InterestDto(Guid Id, string Name, bool IsCustom)
{
    public static InterestDto From(InterestCatalogItemResult result) => new(result.Id, result.Name, result.IsCustom);
}

/// <summary>Группа каталога интересов по категории (T-9.2, <c>GET /api/interests/catalog</c>).</summary>
public sealed record InterestCategoryDto(InterestCategory Category, InterestDto[] Interests)
{
    public static InterestCategoryDto From(InterestCategoryGroupResult result) =>
        new(result.Category, [.. result.Interests.Select(InterestDto.From)]);
}
