using Blizka.App.Auth;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Referrals;
using Blizka.App.Telegram;
using Blizka.App.UseCases.Auth;

namespace Blizka.UnitTests.UseCases.Auth;

public sealed class AuthenticateTelegramUserCommandHandlerTests
{
    private static TelegramInitData MakeInitData(
        long telegramId = 42, string firstName = "Ann", string? lastName = null, string? languageCode = "ru", string? startParam = null) =>
        new(telegramId, firstName, lastName, Username: "ann", PhotoUrl: null, languageCode, DateTimeOffset.UtcNow, startParam);

    [Fact(DisplayName = "КОГДА пользователь авторизуется впервые ТОГДА создаётся новый пользователь со статусом New")]
    public async Task Handle_creates_a_new_active_user_with_status_New()
    {
        var repository = new FakeUserRepository();
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeReferralRepository(), new FakeJwtTokenService());

        var result = await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData(lastName: "K")), CancellationToken.None);

        Assert.True(result.IsNewUser);
        Assert.Equal(UserStatus.New, result.Status);
        var stored = Assert.Single(repository.Users);
        Assert.Equal("Ann K", stored.Name);
        Assert.Equal("ru", stored.Locale);
        Assert.Equal(42, stored.TelegramId);
    }

    [Fact(DisplayName = "КОГДА language_code не поддерживается ТОГДА используется локаль ru")]
    public async Task Handle_falls_back_to_ru_locale_for_unsupported_language_code()
    {
        var repository = new FakeUserRepository();
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeReferralRepository(), new FakeJwtTokenService());

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
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeReferralRepository(), new FakeJwtTokenService());

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
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeReferralRepository(), new FakeJwtTokenService());

        await Assert.ThrowsAsync<UserBannedException>(() =>
            handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА пользователь забанен с причиной и сроком ТОГДА UserBannedException несёт эти значения в Details (spec 002, B2)")]
    public async Task Handle_carries_ban_reason_and_expiry_into_the_exception()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(3);
        var existing = new User
        {
            Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Banned, BanReason = "spam", BannedUntil = expiresAt,
        };
        var repository = new FakeUserRepository(existing);
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeReferralRepository(), new FakeJwtTokenService());

        var exception = await Assert.ThrowsAsync<UserBannedException>(() =>
            handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None));

        Assert.Equal("spam", exception.Details!["reason"]);
        Assert.Equal(expiresAt, exception.Details!["expiresAt"]);
    }

    [Fact(DisplayName = "КОГДА пользователь забанен без причины и срока ТОГДА Details содержит null (бан вручную до T-17.2)")]
    public async Task Handle_carries_null_ban_details_when_not_yet_set()
    {
        var existing = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Banned };
        var repository = new FakeUserRepository(existing);
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeReferralRepository(), new FakeJwtTokenService());

        var exception = await Assert.ThrowsAsync<UserBannedException>(() =>
            handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None));

        Assert.Null(exception.Details!["reason"]);
        Assert.Null(exception.Details!["expiresAt"]);
    }

    [Fact(DisplayName = "КОГДА пользователь авторизуется впервые ТОГДА сохраняется TelegramUsername (spec 002, B6)")]
    public async Task Handle_saves_the_telegram_username_for_a_new_user()
    {
        var repository = new FakeUserRepository();
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeReferralRepository(), new FakeJwtTokenService());

        await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None);

        Assert.Equal("ann", Assert.Single(repository.Users).TelegramUsername);
    }

    [Fact(DisplayName = "КОГДА username в Telegram сменился ТОГДА обновляется при каждом логине (spec 002, B6)")]
    public async Task Handle_updates_the_telegram_username_on_every_login()
    {
        var existing = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Active, TelegramUsername = "old_name" };
        var repository = new FakeUserRepository(existing);
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeReferralRepository(), new FakeJwtTokenService());

        await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None);

        Assert.Equal("ann", existing.TelegramUsername);
    }

    [Fact(DisplayName = "КОГДА пользователь авторизуется ТОГДА результат содержит его locale (spec 002, B7)")]
    public async Task Handle_returns_the_user_locale()
    {
        var repository = new FakeUserRepository();
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeReferralRepository(), new FakeJwtTokenService());

        var result = await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData(languageCode: "be")), CancellationToken.None);

        Assert.Equal("be", result.Locale);
    }

    [Fact(DisplayName = "КОГДА пользователь удалён ТОГДА выбрасывается UserDeletedException")]
    public async Task Handle_throws_UserDeletedException_for_a_deleted_user()
    {
        var existing = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Deleted };
        var repository = new FakeUserRepository(existing);
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeReferralRepository(), new FakeJwtTokenService());

        await Assert.ThrowsAsync<UserDeletedException>(() =>
            handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА при создании пользователя происходит гонка по telegramId ТОГДА возвращается уже созданный конкурентом пользователь без ошибки")]
    public async Task Handle_recovers_from_a_concurrent_user_creation_conflict()
    {
        var repository = new FakeUserRepository();
        var winner = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Active, Name = "Winner", Locale = "ru" };
        repository.ConcurrentWinner = winner;
        var handler = new AuthenticateTelegramUserCommandHandler(repository, new FakeReferralRepository(), new FakeJwtTokenService());

        var result = await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None);

        Assert.False(result.IsNewUser);
        Assert.Equal(winner.Id, result.UserId);
        Assert.Equal(UserStatus.Active, result.Status);
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

    [Fact(DisplayName = "КОГДА новый пользователь авторизуется по реферальной ссылке ТОГДА заводится Referral со статусом Registered (T-20.1)")]
    public async Task Handle_attributes_a_referral_for_a_new_user_with_a_valid_start_param()
    {
        var referrer = new User { Id = Guid.NewGuid(), TelegramId = 7, Status = UserStatus.Active, Name = "Referrer" };
        var repository = new FakeUserRepository(referrer);
        var referralRepository = new FakeReferralRepository();
        var handler = new AuthenticateTelegramUserCommandHandler(repository, referralRepository, new FakeJwtTokenService());
        var startParam = ReferralCodeCodec.StartParamPrefix + ReferralCodeCodec.Encode(referrer.Id);

        var result = await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData(startParam: startParam)), CancellationToken.None);

        var referral = Assert.Single(referralRepository.Referrals);
        Assert.Equal(referrer.Id, referral.ReferrerUserId);
        Assert.Equal(result.UserId, referral.ReferredUserId);
        Assert.Equal(ReferralStatus.Pending, referral.Status);
    }

    [Fact(DisplayName = "КОГДА start_param не содержит валидный реферальный код ТОГДА Referral не создаётся, авторизация проходит как обычно")]
    public async Task Handle_ignores_an_invalid_start_param()
    {
        var repository = new FakeUserRepository();
        var referralRepository = new FakeReferralRepository();
        var handler = new AuthenticateTelegramUserCommandHandler(repository, referralRepository, new FakeJwtTokenService());

        await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData(startParam: "ref_not-a-valid-code")), CancellationToken.None);

        Assert.Empty(referralRepository.Referrals);
    }

    [Fact(DisplayName = "КОГДА реферер по коду не найден ТОГДА Referral не создаётся")]
    public async Task Handle_ignores_a_start_param_for_an_unknown_referrer()
    {
        var repository = new FakeUserRepository();
        var referralRepository = new FakeReferralRepository();
        var handler = new AuthenticateTelegramUserCommandHandler(repository, referralRepository, new FakeJwtTokenService());
        var startParam = ReferralCodeCodec.StartParamPrefix + ReferralCodeCodec.Encode(Guid.NewGuid());

        await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData(startParam: startParam)), CancellationToken.None);

        Assert.Empty(referralRepository.Referrals);
    }

    private sealed class FakeReferralRepository : IReferralRepository
    {
        public List<Referral> Referrals { get; } = [];

        public Task<Referral?> GetByReferredUserIdAsync(Guid referredUserId, CancellationToken cancellationToken) =>
            Task.FromResult(Referrals.SingleOrDefault(r => r.ReferredUserId == referredUserId));

        public Task AddAsync(Referral referral, CancellationToken cancellationToken)
        {
            Referrals.Add(referral);
            return Task.CompletedTask;
        }

        public Task<(int Invited, int Registered)> GetCountsAsync(Guid referrerUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Статистика рефералов не используется в тестах аутентификации.");

        public Task<int> GetTotalSparksEarnedAsync(Guid referrerUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Статистика рефералов не используется в тестах аутентификации.");
    }

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public JwtIssuedToken IssueToken(User user) => new("fake-token", DateTimeOffset.UtcNow.AddHours(24));
    }
}
