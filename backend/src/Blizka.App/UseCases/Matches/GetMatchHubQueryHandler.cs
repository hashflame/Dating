using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Cities;
using Blizka.App.UseCases.Feed;
using Blizka.App.UseCases.Privacy;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Matches;

/// <summary>Обрабатывает <see cref="GetMatchHubQuery"/> (T-7.2) — детальная карточка мэтча.</summary>
public sealed class GetMatchHubQueryHandler(
    IMatchRepository matchRepository, IPrivacySettingsRepository privacySettingsRepository,
    IQuestionOfDayRepository questionOfDayRepository, IOptions<SparksOptions> sparksOptions)
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

        var otherPrivacy = PrivacySettingsDefaults.ToResult(
            await privacySettingsRepository.GetByUserIdAsync(other.Id, cancellationToken));

        var isUnlocked = match.ContactUnlockedAt is not null;
        // writes_first_only (S-51, T-16.1) — второй участник запретил себе писать первым; недостижим для
        // WritesFirst в T-7.1 (GetMatchesQueryHandler) — та же MVP-заглушка там осталась намеренно (out of scope).
        var contactStatus = isUnlocked ? "unlocked" : otherPrivacy.BlockIncomingMessages ? "writes_first_only" : "locked";

        // QuestionOfDay должен отражать реальную доступность (T-11.1: GET .../question-of-day отдаёт
        // available:false/409, пока GenerateQuestionOfDay ни разу не отработал), а не просто "фича включена
        // для пары" — иначе хаб обещает то, чего нет (баг T-7.2).
        var currentQuestion = await questionOfDayRepository.GetCurrentAsync(DateTimeOffset.UtcNow, cancellationToken);
        var questionOfDayAvailability = currentQuestion is null ? NotAvailable : Available;

        return new MatchHubResult(
            match.Id,
            MatchResultMapper.ToHubUserResult(other, isUnlocked, otherPrivacy.ShowLastActive, locale),
            new MatchHubCompatibilityResult(scored.Score, MatchCompatibilityDescriber.Describe(scored, sharedInterestNames)),
            contactStatus,
            sparksOptions.Value.ContactUnlockCost,
            // DateIdea (T-12.1, MVP-заглушка) доступна во всех мэтчах; Minigame/StaleConversation ждут своих
            // задач (T-14.1/T-15.1).
            new MatchHubFeaturesResult(questionOfDayAvailability, NotAvailable, Available, NotAvailable));
    }
}
