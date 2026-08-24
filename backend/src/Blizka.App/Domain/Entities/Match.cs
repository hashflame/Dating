using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

public sealed class Match
{
    public Guid Id { get; set; }

    /// <summary>Меньший из двух id пользователей — перед вставкой вызывающий код должен канонизировать порядок, чтобы unique-индекс (User1Id, User2Id) ловил дубликаты пары независимо от того, кто с кем мэтчнулся.</summary>
    public Guid User1Id { get; set; }

    public User? User1 { get; set; }

    public Guid User2Id { get; set; }

    public User? User2 { get; set; }

    public MatchStatus Status { get; set; } = MatchStatus.Active;

    public DateTimeOffset MatchedAt { get; set; }

    public DateTimeOffset? ContactUnlockedAt { get; set; }

    public Guid? ContactUnlockedByUserId { get; set; }

    public User? ContactUnlockedByUser { get; set; }

    public DateTimeOffset? MessageSentCheckAt { get; set; }

    /// <summary>Момент перевода в архив (T-7.4, ещё не реализована) — источник для <c>archivedAt</c> в T-7.1. Пока ничто не проставляет это поле, кроме тестов/ручной архивации.</summary>
    public DateTimeOffset? ArchivedAt { get; set; }
}
