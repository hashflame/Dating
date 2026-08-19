namespace Blizka.App.Domain.Entities;

public sealed class Minigame
{
    public Guid Id { get; set; }

    public Guid MatchId { get; set; }

    public Match? Match { get; set; }

    /// <summary>Индексы в каталоге дилемм (T-14.1), выбранные для этого экземпляра игры.</summary>
    public int[] DilemmaIds { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
