using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Cities;

/// <summary><c>POST /api/geo/detect</c> (T-4.1) — определение города по координатам устройства.</summary>
public sealed record DetectCityQuery(double Lat, double Lon, CityLocale Locale) : IRequest<GeoDetectResult>;
