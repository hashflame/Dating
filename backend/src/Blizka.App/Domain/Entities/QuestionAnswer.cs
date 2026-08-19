namespace Blizka.App.Domain.Entities;

public sealed class QuestionAnswer
{
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }

    public QuestionOfDay? Question { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Guid MatchId { get; set; }

    public Match? Match { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset AnsweredAt { get; set; }
}
