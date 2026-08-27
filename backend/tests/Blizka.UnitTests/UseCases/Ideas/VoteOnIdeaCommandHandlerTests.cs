using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Ideas;

namespace Blizka.UnitTests.UseCases.Ideas;

public sealed class VoteOnIdeaCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА идея существует ТОГДА голос ставится")]
    public async Task Handle_adds_a_vote_when_the_idea_exists()
    {
        var ideaId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repository = new FakeIdeaRepository { ExistingIdeaIds = [ideaId] };
        var handler = new VoteOnIdeaCommandHandler(repository);

        await handler.Handle(new VoteOnIdeaCommand(userId, ideaId), CancellationToken.None);

        Assert.Equal((ideaId, userId), Assert.Single(repository.Votes));
    }

    [Fact(DisplayName = "КОГДА идеи с таким id нет ТОГДА выбрасывается IdeaNotFoundException и голос не ставится")]
    public async Task Handle_throws_when_the_idea_does_not_exist()
    {
        var repository = new FakeIdeaRepository();
        var handler = new VoteOnIdeaCommandHandler(repository);

        await Assert.ThrowsAsync<IdeaNotFoundException>(
            () => handler.Handle(new VoteOnIdeaCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
        Assert.Empty(repository.Votes);
    }

    private sealed class FakeIdeaRepository : IIdeaRepository
    {
        public HashSet<Guid> ExistingIdeaIds { get; init; } = [];

        public List<(Guid IdeaId, Guid UserId)> Votes { get; } = [];

        public Task<(IReadOnlyList<IdeaListEntry> Items, int TotalCount)> GetPageAsync(
            IdeaListTab tab, Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах голосования.");

        public Task<bool> ExistsAsync(Guid ideaId, CancellationToken cancellationToken) =>
            Task.FromResult(ExistingIdeaIds.Contains(ideaId));

        public Task AddAsync(Idea idea, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах голосования.");

        public Task<bool> AddVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken)
        {
            Votes.Add((ideaId, userId));
            return Task.FromResult(true);
        }

        public Task<bool> RemoveVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах голосования.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах голосования.");
    }
}
