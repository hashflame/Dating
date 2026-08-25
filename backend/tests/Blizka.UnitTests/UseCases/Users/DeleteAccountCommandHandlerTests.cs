using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Users;

namespace Blizka.UnitTests.UseCases.Users;

public sealed class DeleteAccountCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА аккаунт активен ТОГДА статус становится Deleted и проставляется DeletedAt")]
    public async Task Handle_marks_the_user_as_deleted()
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Active };
        var repository = new FakeUserRepository(user);
        var handler = new DeleteAccountCommandHandler(repository);

        await handler.Handle(new DeleteAccountCommand(user.Id), CancellationToken.None);

        Assert.Equal(UserStatus.Deleted, user.Status);
        Assert.NotNull(user.DeletedAt);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА аккаунт уже удалён ТОГДА повторный вызов не бросает исключение и не трогает БД")]
    public async Task Handle_is_idempotent_for_an_already_deleted_account()
    {
        var deletedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var user = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Deleted, DeletedAt = deletedAt };
        var repository = new FakeUserRepository(user);
        var handler = new DeleteAccountCommandHandler(repository);

        await handler.Handle(new DeleteAccountCommand(user.Id), CancellationToken.None);

        Assert.Equal(deletedAt, user.DeletedAt);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА аутентифицированный пользователь не найден в репозитории ТОГДА выбрасывается InvalidOperationException")]
    public async Task Handle_throws_when_the_authenticated_user_is_missing()
    {
        var handler = new DeleteAccountCommandHandler(new FakeUserRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new DeleteAccountCommand(Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class FakeUserRepository(params User[] seed) : IUserRepository
    {
        private readonly List<User> _users = [.. seed];

        public bool SaveChangesCalled { get; private set; }

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteAccountCommandHandler.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteAccountCommandHandler.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteAccountCommandHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
