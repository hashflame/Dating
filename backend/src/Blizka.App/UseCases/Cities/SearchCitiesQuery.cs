using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Cities;

/// <summary><c>GET /api/cities/search</c> (T-4.1) — полнотекстовый поиск городов каталога по подстроке.</summary>
public sealed record SearchCitiesQuery(string Q, CityLocale Locale) : IRequest<IReadOnlyList<CitySearchResult>>;
