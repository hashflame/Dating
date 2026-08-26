using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Blocks;
using FluentValidation;

namespace Blizka.UnitTests.UseCases.Blocks;

public sealed class BlockUserCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА цель блокировки существует и ещё не заблокирована ТОГДА блокировка сохраняется")]
    public async Task Handle_adds_a_block()
    {
        var blocker = Guid.NewGuid();
        var target = new User { Id = Guid.NewGuid(), TelegramId = 1 };
        var userRepository = new FakeUserRepository(target);
        var blockRepository = new FakeUserBlockRepository();
        var handler = new BlockUserCommandHandler(userRepository, blockRepository, new BlockUserCommandValidator());

        await handler.Handle(new BlockUserCommand(blocker, target.Id), CancellationToken.None);

        var added = Assert.Single(blockRepository.AddedBlocks);
        Assert.Equal(blocker, added.BlockerUserId);
        Assert.Equal(target.Id, added.BlockedUserId);
        Assert.True(blockRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА блокировка уже стоит ТОГДА повторный вызов не трогает БД")]
    public async Task Handle_is_idempotent_for_an_already_blocked_user()
    {
        var blocker = Guid.NewGuid();
        var target = new User { Id = Guid.NewGuid(), TelegramId = 1 };
        var userRepository = new FakeUserRepository(target);
        var blockRepository = new FakeUserBlockRepository { AlreadyBlocked = true };
        var handler = new BlockUserCommandHandler(userRepository, blockRepository, new BlockUserCommandValidator());

        await handler.Handle(new BlockUserCommand(blocker, target.Id), CancellationToken.None);

        Assert.Empty(blockRepository.AddedBlocks);
        Assert.False(blockRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА цель блокировки не найдена ТОГДА выбрасывается UserProfileNotFoundException")]
    public async Task Handle_throws_when_the_target_does_not_exist()
    {
        var handler = new BlockUserCommandHandler(new FakeUserRepository(), new FakeUserBlockRepository(), new BlockUserCommandValidator());

        await Assert.ThrowsAsync<UserProfileNotFoundException>(
            () => handler.Handle(new BlockUserCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА пользователь блокирует самого себя ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_when_blocking_self()
    {
        var userId = Guid.NewGuid();
        var handler = new BlockUserCommandHandler(new FakeUserRepository(), new FakeUserBlockRepository(), new BlockUserCommandValidator());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new BlockUserCommand(userId, userId), CancellationToken.None));
    }

    private sealed class FakeUserRepository(params User[] seed) : IUserRepository
    {
        private readonly List<User> _users = [.. seed];

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BlockUserCommandHandler.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BlockUserCommandHandler.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BlockUserCommandHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BlockUserCommandHandler.");
    }

    private sealed class FakeUserBlockRepository : IUserBlockRepository
    {
        public List<UserBlock> AddedBlocks { get; } = [];

        public bool AlreadyBlocked { get; set; }

        public bool SaveChangesCalled { get; private set; }

        public Task<bool> ExistsAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
            Task.FromResult(AlreadyBlocked);

        public Task<bool> ExistsEitherDirectionAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BlockUserCommandHandler.");

        public Task<IReadOnlyList<UserBlock>> GetBlockedByUserAsync(Guid blockerUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BlockUserCommandHandler.");

        public Task AddAsync(UserBlock block, CancellationToken cancellationToken)
        {
            AddedBlocks.Add(block);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BlockUserCommandHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
