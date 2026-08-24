using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Users;

namespace Blizka.UnitTests.UseCases.Users;

public sealed class GetMeQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА пользователь найден ТОГДА возвращаются id, telegramId, имя, баланс зорок, статус и локаль")]
    public async Task Handle_returns_the_users_profile()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 42,
            Name = "Ann",
            SparksBalance = 7,
            Status = UserStatus.Active,
            Locale = "be",
        };
        var handler = new GetMeQueryHandler(new FakeUserRepository(user));

        var result = await handler.Handle(new GetMeQuery(user.Id), CancellationToken.None);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal(42, result.TelegramId);
        Assert.Equal("Ann", result.Name);
        Assert.Equal(7, result.SparksBalance);
        Assert.Equal(UserStatus.Active, result.Status);
        Assert.Equal("be", result.Locale);
    }

    [Fact(DisplayName = "КОГДА аутентифицированный пользователь не найден в репозитории ТОГДА выбрасывается InvalidOperationException")]
    public async Task Handle_throws_when_the_authenticated_user_is_missing()
    {
        var handler = new GetMeQueryHandler(new FakeUserRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetMeQuery(Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class FakeUserRepository(params User[] seed) : IUserRepository
    {
        private readonly List<User> _users = [.. seed];

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetMeQueryHandler.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetMeQueryHandler.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetMeQueryHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetMeQueryHandler.");
    }
}
