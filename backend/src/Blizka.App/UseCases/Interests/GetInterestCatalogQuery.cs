using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Interests;

/// <summary><c>GET /api/interests/catalog</c> (T-9.2) — полный каталог интересов, сгруппированный по категориям.</summary>
public sealed record GetInterestCatalogQuery(CityLocale Locale) : IRequest<IReadOnlyList<InterestCategoryGroupResult>>;

public sealed record InterestCategoryGroupResult(InterestCategory Category, IReadOnlyList<InterestCatalogItemResult> Interests);

public sealed record InterestCatalogItemResult(Guid Id, string Name, bool IsCustom);
