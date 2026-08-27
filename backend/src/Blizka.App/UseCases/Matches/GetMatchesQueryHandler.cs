using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Feed;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Matches;

/// <summary>Обрабатывает <see cref="GetMatchesQuery"/> (T-7.1) — три секции мэтчей с бейджами.</summary>
public sealed class GetMatchesQueryHandler(
    IMatchRepository matchRepository, IPrivacySettingsRepository privacySettingsRepository, IOptions<SparksOptions> sparksOptions)
    : IRequestHandler<GetMatchesQuery, MatchesResult>
{
    // Порог бейджа fire — decomposition.md/spec.md не задают число для «высокого score», выбран по решению
    // продукта при уточнении T-7.1 (шкала FeedCompatibilityScorer — 0-100).
    private const int FireScoreThreshold = 80;

    private const string ContactOpenedBadge = "contact_opened";

    public async Task<MatchesResult> Handle(GetMatchesQuery request, CancellationToken cancellationToken)
    {
        var newMatches = await matchRepository.GetNewAsync(request.UserId, cancellationToken);
        var waiting = await matchRepository.GetWaitingForMessageAsync(request.UserId, cancellationToken);
        var archived = await matchRepository.GetArchivedAsync(request.UserId, cancellationToken);

        // hideAge (T-16.1) — один батч-запрос по всем "другим" участникам трёх секций, а не N+1 (по образцу
        // GetFeedQueryHandler); раньше не читалась вовсе, из-за чего hideAge обходился через списки мэтчей
        // (баг из тикета ClickUp).
        var otherUserIds = newMatches.Concat(waiting).Concat(archived)
            .Select(m => MatchResultMapper.ResolveUsers(m, request.UserId).Other.Id)
            .ToHashSet();
        var privacyByUserId = await privacySettingsRepository.GetByUserIdsAsync(otherUserIds, cancellationToken);

        return new MatchesResult(
            newMatches.Select(m => ToNewResult(m, request.UserId, privacyByUserId)).ToList(),
            waiting.Select(m => ToWaitingResult(m, request.UserId, privacyByUserId)).ToList(),
            archived.Select(m => ToArchivedResult(m, request.UserId, privacyByUserId)).ToList());
    }

    private NewMatchResult ToNewResult(Match match, Guid userId, IReadOnlyDictionary<Guid, PrivacySettings> privacyByUserId)
    {
        var (me, other) = MatchResultMapper.ResolveUsers(match, userId);
        var meInterestIds = me.UserInterests.Select(ui => ui.InterestId).ToHashSet();
        var meDatePreferenceIds = me.UserDatePreferences.Select(p => p.DatePreferenceId).ToHashSet();
        var score = FeedCompatibilityScorer.Score(me, other, meInterestIds, meDatePreferenceIds).Score;

        // T-16.1 (настройки приватности) ещё не реализована — писать первым партнёр всегда "может" (MVP-заглушка),
        // поэтому writesFirst всегда false и бейдж writes_first недостижим.
        var badge = score >= FireScoreThreshold ? "fire" : null;
        var hideAge = privacyByUserId.TryGetValue(other.Id, out var privacy) && privacy.HideAge;

        return new NewMatchResult(
            match.Id, MatchResultMapper.ToUserResult(other, hideAge), match.MatchedAt,
            sparksOptions.Value.ContactUnlockCost, WritesFirst: false, badge);
    }

    private static WaitingMatchResult ToWaitingResult(
        Match match, Guid userId, IReadOnlyDictionary<Guid, PrivacySettings> privacyByUserId)
    {
        var (_, other) = MatchResultMapper.ResolveUsers(match, userId);
        var hideAge = privacyByUserId.TryGetValue(other.Id, out var privacy) && privacy.HideAge;
        return new WaitingMatchResult(
            match.Id, MatchResultMapper.ToUserResult(other, hideAge), match.ContactUnlockedAt!.Value, ContactOpenedBadge);
    }

    private static ArchivedMatchResult ToArchivedResult(
        Match match, Guid userId, IReadOnlyDictionary<Guid, PrivacySettings> privacyByUserId)
    {
        var (_, other) = MatchResultMapper.ResolveUsers(match, userId);
        // ArchivedReason проставляется в момент архивации (ArchiveStaleMatchesJob / ArchiveMatchCommandHandler) —
        // здесь просто читается. Эвристика на MatchArchivalPolicy.IsStale — только фолбэк для мэтчей, у которых
        // почему-то нет ArchivedReason (легаси-данные до этого поля, тестовые фикстуры).
        var reason = match.ArchivedReason
            ?? (MatchArchivalPolicy.IsStale(match.MatchedAt, match.ContactUnlockedAt, match.MessageSentCheckAt, DateTimeOffset.UtcNow)
                ? MatchArchivalPolicy.AutoArchivedReason
                : MatchArchivalPolicy.ManualArchivedReason);
        var hideAge = privacyByUserId.TryGetValue(other.Id, out var privacy) && privacy.HideAge;
        return new ArchivedMatchResult(
            match.Id, MatchResultMapper.ToUserResult(other, hideAge), match.ArchivedAt ?? match.MatchedAt, reason);
    }
}
