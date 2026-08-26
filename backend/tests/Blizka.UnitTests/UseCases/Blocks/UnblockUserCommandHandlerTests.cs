using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Blocks;

namespace Blizka.UnitTests.UseCases.Blocks;

public sealed class UnblockUserCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА снимают блокировку ТОГДА репозиторий вызывается с обоими id")]
    public async Task Handle_removes_the_block()
    {
        var blocker = Guid.NewGuid();
        var blocked = Guid.NewGuid();
        var repository = new FakeUserBlockRepository();
        var handler = new UnblockUserCommandHandler(repository);

        await handler.Handle(new UnblockUserCommand(blocker, blocked), CancellationToken.None);

        Assert.Equal((blocker, blocked), repository.Removed);
    }

    private sealed class FakeUserBlockRepository : IUserBlockRepository
    {
        public (Guid BlockerUserId, Guid BlockedUserId)? Removed { get; private set; }

        public Task<bool> ExistsAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах UnblockUserCommandHandler.");

        public Task<bool> ExistsEitherDirectionAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах UnblockUserCommandHandler.");

        public Task<IReadOnlyList<UserBlock>> GetBlockedByUserAsync(Guid blockerUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах UnblockUserCommandHandler.");

        public Task AddAsync(UserBlock block, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах UnblockUserCommandHandler.");

        public Task RemoveAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken)
        {
            Removed = (blockerUserId, blockedUserId);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах UnblockUserCommandHandler.");
    }
}
