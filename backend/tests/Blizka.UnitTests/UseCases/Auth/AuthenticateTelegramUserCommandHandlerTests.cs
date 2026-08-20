using Blizka.App.Auth;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Telegram;
using Blizka.App.UseCases.Auth;

namespace Blizka.UnitTests.UseCases.Auth;

public sealed class AuthenticateTelegramUserCommandHandlerTests
{
    private static TelegramInitData MakeInitData(long telegramId = 42, string firstName = "Ann", string? lastName = null, string? languageCode = "ru") =>
        new(telegramId, firstName, lastName, Username: "ann", PhotoUrl: null, languageCode, DateTimeOffset.UtcNow);

    [Fact(DisplayName = "КОГДА пользователь авторизуется впервые ТОГДА создаётся новый пользователь со статусом New")]
    public async Task Handle_creates_a_new_active_user_with_status_New()
    {
        var repository = new FakeUserRepository();
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeJwtTokenService());

        var result = await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData(lastName: "K")), CancellationToken.None);

        Assert.True(result.IsNewUser);
        Assert.Equal("New", result.Status);
        var stored = Assert.Single(repository.Users);
        Assert.Equal("Ann K", stored.Name);
        Assert.Equal("ru", stored.Locale);
        Assert.Equal(42, stored.TelegramId);
    }

    [Fact(DisplayName = "КОГДА language_code не поддерживается ТОГДА используется локаль ru")]
    public async Task Handle_falls_back_to_ru_locale_for_unsupported_language_code()
    {
        var repository = new FakeUserRepository();
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeJwtTokenService());

        await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData(languageCode: "fr-FR")), CancellationToken.None);

        Assert.Equal("ru", Assert.Single(repository.Users).Locale);
    }

    [Fact(DisplayName = "КОГДА авторизуется существующий активный пользователь ТОГДА обновляется LastActiveAt")]
    public async Task Handle_updates_LastActiveAt_for_an_existing_active_user()
    {
        var existing = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 42,
            Status = UserStatus.Active,
            Name = "Existing",
            LastActiveAt = DateTimeOffset.UtcNow.AddDays(-3),
        };
        var repository = new FakeUserRepository(existing);
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeJwtTokenService());

        var result = await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None);

        Assert.False(result.IsNewUser);
        Assert.Equal(existing.Id, result.UserId);
        Assert.True(existing.LastActiveAt > DateTimeOffset.UtcNow.AddMinutes(-1));
    }

    [Fact(DisplayName = "КОГДА пользователь забанен ТОГДА выбрасывается UserBannedException")]
    public async Task Handle_throws_UserBannedException_for_a_banned_user()
    {
        var existing = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Banned };
        var repository = new FakeUserRepository(existing);
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeJwtTokenService());

        await Assert.ThrowsAsync<UserBannedException>(() =>
            handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА пользователь удалён ТОГДА выбрасывается UserDeletedException")]
    public async Task Handle_throws_UserDeletedException_for_a_deleted_user()
    {
        var existing = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Deleted };
        var repository = new FakeUserRepository(existing);
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeJwtTokenService());

        await Assert.ThrowsAsync<UserDeletedException>(() =>
            handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА при создании пользователя происходит гонка по telegramId ТОГДА возвращается уже созданный конкурентом пользователь без ошибки")]
    public async Task Handle_recovers_from_a_concurrent_user_creation_conflict()
    {
        var repository = new FakeUserRepository();
        var winner = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Active, Name = "Winner", Locale = "ru" };
        repository.ConcurrentWinner = winner;
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeJwtTokenService());

        var result = await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None);

        Assert.False(result.IsNewUser);
        Assert.Equal(winner.Id, result.UserId);
        Assert.Equal("Active", result.Status);
        Assert.Single(repository.Users);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _pending = [];

        public List<User> Users { get; } = [];

        /// <summary>Когда задано, следующий SaveChangesAsync симулирует конкурентную вставку такого же telegramId: "чужой" пользователь фиксируется в БД первым, а наша попытка сохранить нового падает с ConcurrentUserCreationException.</summary>
        public User? ConcurrentWinner { get; set; }

        public FakeUserRepository(params User[] seed) => Users.AddRange(seed);

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(u => u.TelegramId == telegramId));

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            _pending.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (ConcurrentWinner is { } winner)
            {
                ConcurrentWinner = null;
                Users.Add(winner);
                _pending.Clear();
                throw new ConcurrentUserCreationException(winner.TelegramId, new InvalidOperationException("simulated unique violation"));
            }

            Users.AddRange(_pending);
            _pending.Clear();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public JwtIssuedToken IssueToken(User user) => new("fake-token", DateTimeOffset.UtcNow.AddHours(24));
    }
}
