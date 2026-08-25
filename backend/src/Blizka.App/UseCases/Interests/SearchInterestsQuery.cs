using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Interests;

/// <summary><c>GET /api/interests/search?q=...</c> (T-9.2) — поиск по каталогу (включая ранее созданные кастомные интересы), по образцу <see cref="Cities.SearchCitiesQuery"/>.</summary>
public sealed record SearchInterestsQuery(string Q, CityLocale Locale) : IRequest<IReadOnlyList<InterestCatalogItemResult>>;
