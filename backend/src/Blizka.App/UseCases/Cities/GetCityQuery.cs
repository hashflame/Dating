using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Cities;

/// <summary><c>GET /api/cities/{cityId}</c> (T-4.1) — город по id, чтобы показать название сохранённого <c>cityId</c> на клиенте.</summary>
public sealed record GetCityQuery(Guid CityId, CityLocale Locale) : IRequest<CitySearchResult>;
