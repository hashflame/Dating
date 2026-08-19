namespace Blizka.App.Domain.Entities;

public sealed class IdeaVote
{
    public Guid IdeaId { get; set; }

    public Idea? Idea { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
