using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Ideas;

namespace Blizka.UnitTests.UseCases.Ideas;

public sealed class RemoveIdeaVoteCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА голос есть ТОГДА он снимается")]
    public async Task Handle_removes_the_vote()
    {
        var ideaId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var repository = new FakeIdeaRepository();
        var handler = new RemoveIdeaVoteCommandHandler(repository);

        await handler.Handle(new RemoveIdeaVoteCommand(userId, ideaId), CancellationToken.None);

        Assert.Equal((ideaId, userId), Assert.Single(repository.RemovedVotes));
    }

    [Fact(DisplayName = "КОГДА идеи с таким id нет ТОГДА всё равно успех — не проверяет существование (идемпотентность DELETE)")]
    public async Task Handle_does_not_check_idea_existence()
    {
        var repository = new FakeIdeaRepository();
        var handler = new RemoveIdeaVoteCommandHandler(repository);

        await handler.Handle(new RemoveIdeaVoteCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.Single(repository.RemovedVotes);
    }

    private sealed class FakeIdeaRepository : IIdeaRepository
    {
        public List<(Guid IdeaId, Guid UserId)> RemovedVotes { get; } = [];

        public Task<(IReadOnlyList<IdeaListEntry> Items, int TotalCount)> GetPageAsync(
            IdeaListTab tab, Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах снятия голоса.");

        public Task<bool> ExistsAsync(Guid ideaId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах снятия голоса.");

        public Task AddAsync(Idea idea, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах снятия голоса.");

        public Task<bool> AddVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах снятия голоса.");

        public Task<bool> RemoveVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken)
        {
            RemovedVotes.Add((ideaId, userId));
            return Task.FromResult(true);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах снятия голоса.");
    }
}
