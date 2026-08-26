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

    /// <summary>Момент перевода в архив (T-7.4) — источник для <c>archivedAt</c> в T-7.1.</summary>
    public DateTimeOffset? ArchivedAt { get; set; }

    /// <summary>
    /// Причина архивации (T-7.4) — <see cref="Blizka.App.UseCases.Matches.MatchArchivalPolicy.AutoArchivedReason"/>
    /// или <see cref="Blizka.App.UseCases.Matches.MatchArchivalPolicy.ManualArchivedReason"/>, проставляется в момент
    /// перехода в <see cref="MatchStatus.Archived"/> и там же (не эвристикой на момент чтения — иначе у мэтча,
    /// заархивированного вручную заранее, причина задним числом «протухала» бы в <c>"no_activity_7_days"</c>,
    /// как только реально проходило 7 дней). <c>null</c>, пока мэтч не заархивирован.
    /// </summary>
    public string? ArchivedReason { get; set; }

    /// <summary>Момент подтверждения договорённости о встрече (T-12.1, S-39) — <c>POST /date-confirmed</c>. <c>null</c>, пока не подтверждено.</summary>
    public DateTimeOffset? DateConfirmedAt { get; set; }

    public Guid? DateConfirmedByUserId { get; set; }

    public User? DateConfirmedByUser { get; set; }
}
