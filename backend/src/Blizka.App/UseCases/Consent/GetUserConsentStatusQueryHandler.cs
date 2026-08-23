using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Consent;

/// <summary>
/// Обрабатывает <see cref="GetUserConsentStatusQuery"/> (T-2.2). Лог согласий append-only — по каждому типу
/// берётся самая свежая запись по <see cref="UserConsent.Timestamp"/>, а не первая попавшаяся.
/// </summary>
public sealed class GetUserConsentStatusQueryHandler(IUserConsentRepository consentRepository)
    : IRequestHandler<GetUserConsentStatusQuery, IReadOnlyList<UserConsentStatusResult>>
{
    public async Task<IReadOnlyList<UserConsentStatusResult>> Handle(GetUserConsentStatusQuery request, CancellationToken cancellationToken)
    {
        var consents = await consentRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        var latestByType = consents
            .GroupBy(c => c.Type)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.Timestamp).First());

        return Enum.GetValues<ConsentType>()
            .Select(type => latestByType.TryGetValue(type, out var consent)
                ? new UserConsentStatusResult(type, Given: true, consent.Version, consent.Timestamp)
                : new UserConsentStatusResult(type, Given: false, Version: null, Timestamp: null))
            .ToList();
    }
}
