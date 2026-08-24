namespace Blizka.App.UseCases.Matches;

/// <summary>
/// Порог протухания мэтча (T-7.4) — общий для фоновой джобы <c>ArchiveStaleMatches</c>
/// (<see cref="Blizka.App.Domain.Repositories.IMatchRepository.ArchiveStaleMatchesAsync"/>, где условие переписано
/// в LINQ-предикат для <c>ExecuteUpdateAsync</c>) и для эвристики причины архивации в <see cref="GetMatchesQueryHandler"/>
/// (различает автоархивацию джобой и ручной <c>POST /archive</c> по тому, попадает ли текущее состояние мэтча
/// под условие протухания прямо сейчас).
/// </summary>
public static class MatchArchivalPolicy
{
    public static readonly TimeSpan StaleAfter = TimeSpan.FromDays(7);

    /// <summary>Джоба ArchiveStaleMatches заархивировала мэтч по условию протухания — единственное описанное в spec.md 8.1 значение.</summary>
    public const string AutoArchivedReason = "no_activity_7_days";

    /// <summary>Пользователь заархивировал мэтч вручную (<c>POST /archive</c>) раньше, чем тот подпал под условие протухания; спекой отдельно не размечено.</summary>
    public const string ManualArchivedReason = "manual";

    /// <summary>Мэтч без открытого контакта протух через <see cref="StaleAfter"/> после <c>MatchedAt</c>; с открытым — через <see cref="StaleAfter"/> после <c>ContactUnlockedAt</c>, если так и не было <c>message-sent-check</c>.</summary>
    public static bool IsStale(DateTimeOffset matchedAt, DateTimeOffset? contactUnlockedAt, DateTimeOffset? messageSentCheckAt, DateTimeOffset now) =>
        contactUnlockedAt is null
            ? now - matchedAt > StaleAfter
            : messageSentCheckAt is null && now - contactUnlockedAt.Value > StaleAfter;
}
