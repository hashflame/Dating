namespace Blizka.App.Domain.Entities;

public sealed class MinigameAnswer
{
    public Guid Id { get; set; }

    public Guid MinigameId { get; set; }

    public Minigame? Minigame { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public int DilemmaIndex { get; set; }

    public char Choice { get; set; }

    public DateTimeOffset AnsweredAt { get; set; }
}
