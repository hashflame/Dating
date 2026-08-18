using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

public sealed class Idea
{
    public Guid Id { get; set; }

    public Guid AuthorUserId { get; set; }

    public User? AuthorUser { get; set; }

    public string Text { get; set; } = string.Empty;

    public bool IsAnonymous { get; set; }

    public IdeaStatus Status { get; set; } = IdeaStatus.New;

    public int VotesCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<IdeaVote> Votes { get; set; } = [];
}
