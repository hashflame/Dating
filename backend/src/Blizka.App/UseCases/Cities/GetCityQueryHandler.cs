using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Cities;

/// <summary>Обрабатывает <see cref="GetCityQuery"/> (T-4.1) — прямой поиск по id, в отличие от <see cref="SearchCitiesQueryHandler"/>.</summary>
public sealed class GetCityQueryHandler(ICityRepository cityRepository) : IRequestHandler<GetCityQuery, CitySearchResult>
{
    public async Task<CitySearchResult> Handle(GetCityQuery request, CancellationToken cancellationToken)
    {
        var city = await cityRepository.GetByIdAsync(request.CityId, cancellationToken)
            ?? throw new CityNotFoundException(request.CityId);

        return new CitySearchResult(city.Id, CityNameResolver.Resolve(city, request.Locale), city.Country, city.IsOpen);
    }
}
