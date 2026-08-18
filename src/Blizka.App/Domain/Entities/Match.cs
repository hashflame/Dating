using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

public sealed class Match
{
    public Guid Id { get; set; }

    /// <summary>The smaller of the two user ids — callers must canonicalize ordering before insert so the unique (User1Id, User2Id) index catches duplicate pairs regardless of who matched whom.</summary>
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
}
