using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Users;

namespace Blizka.UnitTests.UseCases.Users;

public sealed class PauseAccountCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА аккаунт активен ТОГДА статус становится Paused")]
    public async Task Handle_marks_the_user_as_paused()
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Active };
        var repository = new FakeUserRepository(user);
        var handler = new PauseAccountCommandHandler(repository);

        await handler.Handle(new PauseAccountCommand(user.Id), CancellationToken.None);

        Assert.Equal(UserStatus.Paused, user.Status);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА аккаунт уже на паузе ТОГДА повторный вызов не трогает БД")]
    public async Task Handle_is_idempotent_for_an_already_paused_account()
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Paused };
        var repository = new FakeUserRepository(user);
        var handler = new PauseAccountCommandHandler(repository);

        await handler.Handle(new PauseAccountCommand(user.Id), CancellationToken.None);

        Assert.False(repository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА аутентифицированный пользователь не найден в репозитории ТОГДА выбрасывается InvalidOperationException")]
    public async Task Handle_throws_when_the_authenticated_user_is_missing()
    {
        var handler = new PauseAccountCommandHandler(new FakeUserRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new PauseAccountCommand(Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class FakeUserRepository(params User[] seed) : IUserRepository
    {
        private readonly List<User> _users = [.. seed];

        public bool SaveChangesCalled { get; private set; }

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах PauseAccountCommandHandler.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах PauseAccountCommandHandler.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах PauseAccountCommandHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
