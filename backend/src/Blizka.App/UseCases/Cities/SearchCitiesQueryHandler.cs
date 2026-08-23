using Blizka.App.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Cities;

/// <summary>Обрабатывает <see cref="SearchCitiesQuery"/> (T-4.1) — trigram-поиск через <see cref="ICityRepository.SearchAsync"/>.</summary>
public sealed class SearchCitiesQueryHandler(ICityRepository cityRepository, IValidator<SearchCitiesQuery> validator)
    : IRequestHandler<SearchCitiesQuery, IReadOnlyList<CitySearchResult>>
{
    private const int Limit = 10;

    public async Task<IReadOnlyList<CitySearchResult>> Handle(SearchCitiesQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var cities = await cityRepository.SearchAsync(request.Q.Trim(), request.Locale, Limit, cancellationToken);

        return cities
            .Select(c => new CitySearchResult(c.Id, CityNameResolver.Resolve(c, request.Locale), c.Country, c.IsOpen, c.Region, c.Type))
            .ToList();
    }
}
