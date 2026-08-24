namespace Blizka.App.UseCases.Matches;

/// <summary>Результат <c>GET /api/matches</c> (T-7.1, spec.md 8.1) — три секции.</summary>
public sealed record MatchesResult(
    IReadOnlyList<NewMatchResult> New,
    IReadOnlyList<WaitingMatchResult> WaitingForMessage,
    IReadOnlyList<ArchivedMatchResult> Archived);

/// <param name="ContactCost">Стоимость открытия контакта в зорках (<c>Sparks:ContactUnlockCost</c>).</param>
/// <param name="WritesFirst">
/// Настройка приватности партнёра «Запретить писать мне в Telegram» (S-51, T-16.1). T-16.1 ещё не реализована —
/// всегда <c>false</c> до появления источника данных, по аналогии с MVP-заглушками недостающих веток в T-7.2.
/// </param>
/// <param name="Badge"><c>"fire"</c> при высоком score совместимости, иначе <c>null</c>. <c>"writes_first"</c> недостижим, пока <see cref="WritesFirst"/> всегда false.</param>
public sealed record NewMatchResult(
    Guid MatchId, MatchUserResult User, DateTimeOffset MatchedAt, int ContactCost, bool WritesFirst, string? Badge);

public sealed record WaitingMatchResult(Guid MatchId, MatchUserResult User, DateTimeOffset ContactOpenedAt, string Badge);

/// <param name="Reason">
/// Единственное описанное в spec.md 8.1 значение — <c>"no_activity_7_days"</c>. Реальная архивация (T-7.4) ещё не
/// реализована, второй сценарий из decomposition.md (контакт открыт, но нет message-sent-check &gt; 7 дней)
/// отдельным значением спекой не размечен.
/// </param>
public sealed record ArchivedMatchResult(Guid MatchId, MatchUserResult User, DateTimeOffset ArchivedAt, string Reason);
