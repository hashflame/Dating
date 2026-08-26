using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.DatePreferences;

/// <summary><c>GET /api/date-preferences/catalog</c> (T-9.3) — фиксированный каталог из 4 предпочтений по формату свидания.</summary>
public sealed record GetDatePreferenceCatalogQuery(CityLocale Locale) : IRequest<IReadOnlyList<DatePreferenceCatalogItemResult>>;

public sealed record DatePreferenceCatalogItemResult(Guid Id, DatePreferenceCode Code, string Name);
