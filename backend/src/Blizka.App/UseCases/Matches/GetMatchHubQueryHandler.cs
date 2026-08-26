using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Cities;
using Blizka.App.UseCases.Feed;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Matches;

/// <summary>Обрабатывает <see cref="GetMatchHubQuery"/> (T-7.2) — детальная карточка мэтча.</summary>
public sealed class GetMatchHubQueryHandler(IMatchRepository matchRepository, IOptions<SparksOptions> sparksOptions)
    : IRequestHandler<GetMatchHubQuery, MatchHubResult>
{
    private static readonly FeatureAvailabilityResult NotAvailable = new(false);
    private static readonly FeatureAvailabilityResult Available = new(true);

    public async Task<MatchHubResult> Handle(GetMatchHubQuery request, CancellationToken cancellationToken)
    {
        var match = await matchRepository.GetByIdForUserAsync(request.MatchId, request.UserId, cancellationToken)
            ?? throw new MatchNotFoundException(request.MatchId);

        var (me, other) = MatchResultMapper.ResolveUsers(match, request.UserId);
        var meInterestIds = me.UserInterests.Select(ui => ui.InterestId).ToHashSet();
        var meDatePreferenceIds = me.UserDatePreferences.Select(p => p.DatePreferenceId).ToHashSet();
        var scored = FeedCompatibilityScorer.Score(me, other, meInterestIds, meDatePreferenceIds);

        var locale = CityLocaleResolver.Resolve(me.Locale);
        var sharedInterestNames = other.UserInterests
            .Where(ui => scored.SharedInterestIds.Contains(ui.InterestId))
            .Select(ui => InterestNameResolver.Resolve(ui.Interest!, locale))
            .ToList();

        var isUnlocked = match.ContactUnlockedAt is not null;
        // writes_first_only (S-51) недостижим до T-16.1 (PrivacySettings ещё нет) — тот же MVP-приём,
        // что WritesFirst=false в T-7.1 (см. GetMatchesQueryHandler).
        var contactStatus = isUnlocked ? "unlocked" : "locked";

        return new MatchHubResult(
            match.Id,
            MatchResultMapper.ToHubUserResult(other, isUnlocked, locale),
            new MatchHubCompatibilityResult(scored.Score, MatchCompatibilityDescriber.Describe(scored, sharedInterestNames)),
            contactStatus,
            sparksOptions.Value.ContactUnlockCost,
            // QuestionOfDay — T-11.1 реализована, доступна во всех мэтчах; остальные три ветки ждут своих задач.
            new MatchHubFeaturesResult(Available, NotAvailable, NotAvailable, NotAvailable));
    }
}
