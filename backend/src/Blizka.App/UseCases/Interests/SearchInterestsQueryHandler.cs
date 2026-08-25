using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Feed;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Interests;

/// <summary>Обрабатывает <see cref="SearchInterestsQuery"/> (T-9.2) — trigram-поиск через <see cref="IInterestRepository.SearchAsync"/>, по образцу <see cref="Cities.SearchCitiesQueryHandler"/>.</summary>
public sealed class SearchInterestsQueryHandler(IInterestRepository interestRepository, IValidator<SearchInterestsQuery> validator)
    : IRequestHandler<SearchInterestsQuery, IReadOnlyList<InterestCatalogItemResult>>
{
    private const int Limit = 10;

    public async Task<IReadOnlyList<InterestCatalogItemResult>> Handle(SearchInterestsQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var interests = await interestRepository.SearchAsync(request.Q.Trim(), request.Locale, Limit, cancellationToken);

        return interests
            .Select(i => new InterestCatalogItemResult(i.Id, InterestNameResolver.Resolve(i, request.Locale), i.IsCustom))
            .ToList();
    }
}
