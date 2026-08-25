using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Feed;
using MediatR;

namespace Blizka.App.UseCases.Interests;

/// <summary>Обрабатывает <see cref="GetInterestCatalogQuery"/> (T-9.2) — полный каталог, сгруппированный по <see cref="Domain.Enums.InterestCategory"/> в порядке объявления enum.</summary>
public sealed class GetInterestCatalogQueryHandler(IInterestRepository interestRepository)
    : IRequestHandler<GetInterestCatalogQuery, IReadOnlyList<InterestCategoryGroupResult>>
{
    public async Task<IReadOnlyList<InterestCategoryGroupResult>> Handle(GetInterestCatalogQuery request, CancellationToken cancellationToken)
    {
        var interests = await interestRepository.GetCatalogAsync(cancellationToken);

        return interests
            .GroupBy(i => i.Category)
            .OrderBy(g => (int)g.Key)
            .Select(g => new InterestCategoryGroupResult(
                g.Key,
                g.Select(i => new InterestCatalogItemResult(i.Id, InterestNameResolver.Resolve(i, request.Locale), i.IsCustom))
                    .OrderBy(i => i.Name)
                    .ToList()))
            .ToList();
    }
}
