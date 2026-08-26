using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Users;

namespace Blizka.UnitTests.UseCases.Users;

public sealed class ResumeAccountCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА аккаунт на паузе ТОГДА статус становится Active")]
    public async Task Handle_marks_the_user_as_active()
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Paused };
        var repository = new FakeUserRepository(user);
        var handler = new ResumeAccountCommandHandler(repository);

        await handler.Handle(new ResumeAccountCommand(user.Id), CancellationToken.None);

        Assert.Equal(UserStatus.Active, user.Status);
        Assert.True(repository.SaveChangesCalled);
    }

    [Theory(DisplayName = "КОГДА аккаунт не на паузе (Deleted/Banned) ТОГДА статус не меняется и БД не трогается")]
    [InlineData(UserStatus.Deleted)]
    [InlineData(UserStatus.Banned)]
    [InlineData(UserStatus.Active)]
    public async Task Handle_does_nothing_when_the_account_is_not_paused(UserStatus status)
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = status };
        var repository = new FakeUserRepository(user);
        var handler = new ResumeAccountCommandHandler(repository);

        await handler.Handle(new ResumeAccountCommand(user.Id), CancellationToken.None);

        Assert.Equal(status, user.Status);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА аутентифицированный пользователь не найден в репозитории ТОГДА выбрасывается InvalidOperationException")]
    public async Task Handle_throws_when_the_authenticated_user_is_missing()
    {
        var handler = new ResumeAccountCommandHandler(new FakeUserRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new ResumeAccountCommand(Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class FakeUserRepository(params User[] seed) : IUserRepository
    {
        private readonly List<User> _users = [.. seed];

        public bool SaveChangesCalled { get; private set; }

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ResumeAccountCommandHandler.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ResumeAccountCommandHandler.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ResumeAccountCommandHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
