using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.DatePreferences;

/// <summary>Обрабатывает <see cref="GetDatePreferenceCatalogQuery"/> (T-9.3) — каталог, отсортированный по <see cref="Domain.Enums.DatePreferenceCode"/> в порядке объявления enum.</summary>
public sealed class GetDatePreferenceCatalogQueryHandler(IUserDatePreferenceRepository datePreferenceRepository)
    : IRequestHandler<GetDatePreferenceCatalogQuery, IReadOnlyList<DatePreferenceCatalogItemResult>>
{
    public async Task<IReadOnlyList<DatePreferenceCatalogItemResult>> Handle(
        GetDatePreferenceCatalogQuery request, CancellationToken cancellationToken)
    {
        var preferences = await datePreferenceRepository.GetCatalogAsync(cancellationToken);

        return preferences
            .OrderBy(p => (int)p.Code)
            .Select(p => new DatePreferenceCatalogItemResult(p.Id, p.Code, DatePreferenceNameResolver.Resolve(p, request.Locale)))
            .ToList();
    }
}
