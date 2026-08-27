using Blizka.App.Auth;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Referrals;
using Blizka.App.Sparks;
using Blizka.App.Telegram;
using Blizka.App.UseCases.Auth;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.Auth;

public sealed class AuthenticateTelegramUserCommandHandlerTests
{
    private static TelegramInitData MakeInitData(
        long telegramId = 42, string firstName = "Ann", string? lastName = null, string? languageCode = "ru", string? startParam = null) =>
        new(telegramId, firstName, lastName, Username: "ann", PhotoUrl: null, languageCode, DateTimeOffset.UtcNow, startParam);

    private static AuthenticateTelegramUserCommandHandler CreateHandler(
        FakeUserRepository userRepository, IReferralRepository? referralRepository = null) =>
        new(
            userRepository,
            referralRepository ?? new FakeReferralRepository(),
            new FakeSwipeRepository(),
            new FakeMatchRepository(),
            new FakePhotoRepository(),
            new SparksService(new FakeSparkTransactionRepository(), userRepository),
            Options.Create(new SparksOptions { RegistrationBonusAmount = 50 }),
            new FakeJwtTokenService());

    [Fact(DisplayName = "КОГДА пользователь авторизуется впервые ТОГДА создаётся новый пользователь со статусом New")]
    public async Task Handle_creates_a_new_active_user_with_status_New()
    {
        var repository = new FakeUserRepository();
        var handler = CreateHandler(repository);

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
        var handler = CreateHandler(repository);

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
        var handler = CreateHandler(repository);

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
        var handler = CreateHandler(repository);

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
        var handler = CreateHandler(repository);

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
        var handler = CreateHandler(repository);

        var exception = await Assert.ThrowsAsync<UserBannedException>(() =>
            handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None));

        Assert.Null(exception.Details!["reason"]);
        Assert.Null(exception.Details!["expiresAt"]);
    }

    [Fact(DisplayName = "КОГДА пользователь авторизуется впервые ТОГДА сохраняется TelegramUsername (spec 002, B6)")]
    public async Task Handle_saves_the_telegram_username_for_a_new_user()
    {
        var repository = new FakeUserRepository();
        var handler = CreateHandler(repository);

        await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None);

        Assert.Equal("ann", Assert.Single(repository.Users).TelegramUsername);
    }

    [Fact(DisplayName = "КОГДА username в Telegram сменился ТОГДА обновляется при каждом логине (spec 002, B6)")]
    public async Task Handle_updates_the_telegram_username_on_every_login()
    {
        var existing = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Active, TelegramUsername = "old_name" };
        var repository = new FakeUserRepository(existing);
        var handler = CreateHandler(repository);

        await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None);

        Assert.Equal("ann", existing.TelegramUsername);
    }

    [Fact(DisplayName = "КОГДА пользователь авторизуется ТОГДА результат содержит его locale (spec 002, B7)")]
    public async Task Handle_returns_the_user_locale()
    {
        var repository = new FakeUserRepository();
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData(languageCode: "be")), CancellationToken.None);

        Assert.Equal("be", result.Locale);
    }

    [Fact(DisplayName = "КОГДА пользователь ранее удалил аккаунт и входит снова ТОГДА он поднимается в статус New, а не 410 (тикет ClickUp)")]
    public async Task Handle_revives_a_deleted_account_instead_of_throwing()
    {
        var existing = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 42,
            Status = UserStatus.Deleted,
            DeletedAt = DateTimeOffset.UtcNow.AddDays(-1),
            RegistrationBonusAwardedAt = DateTimeOffset.UtcNow.AddDays(-30),
            CompletenessBonus60AwardedAt = DateTimeOffset.UtcNow.AddDays(-30),
            SparksBalance = 3,
        };
        var repository = new FakeUserRepository(existing);
        var handler = CreateHandler(repository);

        var result = await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None);

        Assert.True(result.IsNewUser);
        Assert.Equal(UserStatus.New, result.Status);
        Assert.Equal(UserStatus.New, existing.Status);
        Assert.Null(existing.DeletedAt);
    }

    [Fact(DisplayName = "КОГДА восстановленный аккаунт уже получал регистрационный бонус ТОГДА баланс возвращается к этой сумме через леджер, бонус повторно не начисляется")]
    public async Task Handle_resets_the_balance_of_a_revived_account_through_the_ledger_without_re_awarding_the_bonus()
    {
        var registrationBonusAwardedAt = DateTimeOffset.UtcNow.AddDays(-30);
        var existing = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 42,
            Status = UserStatus.Deleted,
            RegistrationBonusAwardedAt = registrationBonusAwardedAt,
            CompletenessBonus100AwardedAt = DateTimeOffset.UtcNow.AddDays(-30),
            SparksBalance = 999,
        };
        var repository = new FakeUserRepository(existing);
        var transactionRepository = new FakeSparkTransactionRepository();
        var handler = new AuthenticateTelegramUserCommandHandler(
            repository,
            new FakeReferralRepository(),
            new FakeSwipeRepository(),
            new FakeMatchRepository(),
            new FakePhotoRepository(),
            new SparksService(transactionRepository, repository),
            Options.Create(new SparksOptions { RegistrationBonusAmount = 50 }),
            new FakeJwtTokenService());

        await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None);

        // RegistrationBonusAwardedAt/CompletenessBonus100AwardedAt намеренно не трогаются — иначе цикл
        // "удалил → вошёл → снова заработал" фармил бы зорки бесконечно (в отличие от dev-инструмента
        // ResetUserStateCommandHandler, где эти поля сознательно сбрасываются для повторного тестирования).
        Assert.Equal(50, existing.SparksBalance);
        Assert.Equal(registrationBonusAwardedAt, existing.RegistrationBonusAwardedAt);
        Assert.NotNull(existing.CompletenessBonus100AwardedAt);

        // Баланс меняется через леджер (AdjustAsync), а не напрямую полем — иначе кошелёк расходится с
        // журналом операций (тот же баг, что чинили в ResetUserStateCommandHandler).
        var transaction = Assert.Single(transactionRepository.Added);
        Assert.Equal(SparkTransactionType.AccountRevival, transaction.Type);
        Assert.Equal(50 - 999, transaction.Amount);
        Assert.Equal(50, transaction.BalanceAfter);
    }

    [Fact(DisplayName = "КОГДА восстановленный аккаунт удаляется навсегда ТОГДА свайпы/мэтчи/фото очищаются, как при dev-сбросе состояния")]
    public async Task Handle_clears_activity_and_optional_profile_fields_of_a_revived_account()
    {
        var existing = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Deleted, Bio = "old bio", IsVerified = true };
        var repository = new FakeUserRepository(existing);
        var swipeRepository = new FakeSwipeRepository();
        var matchRepository = new FakeMatchRepository();
        var handler = new AuthenticateTelegramUserCommandHandler(
            repository,
            new FakeReferralRepository(),
            swipeRepository,
            matchRepository,
            new FakePhotoRepository(),
            new SparksService(new FakeSparkTransactionRepository(), repository),
            Options.Create(new SparksOptions { RegistrationBonusAmount = 50 }),
            new FakeJwtTokenService());

        await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData()), CancellationToken.None);

        Assert.Equal(existing.Id, swipeRepository.RemovedInvolvingUserId);
        Assert.Equal(existing.Id, matchRepository.RemovedForUserId);
        Assert.Null(existing.Bio);
        Assert.False(existing.IsVerified);
    }

    [Fact(DisplayName = "КОГДА при создании пользователя происходит гонка по telegramId ТОГДА возвращается уже созданный конкурентом пользователь без ошибки")]
    public async Task Handle_recovers_from_a_concurrent_user_creation_conflict()
    {
        var repository = new FakeUserRepository();
        var winner = new User { Id = Guid.NewGuid(), TelegramId = 42, Status = UserStatus.Active, Name = "Winner", Locale = "ru" };
        repository.ConcurrentWinner = winner;
        var handler = CreateHandler(repository);

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

    private sealed class FakeSwipeRepository : ISwipeRepository
    {
        public Guid? RemovedInvolvingUserId { get; private set; }

        public Task<bool> ExistsActiveAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<bool> HasActiveMutualLikeAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<Swipe?> GetLastActiveAsync(Guid fromUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<int> CountUndoneSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<int> CountSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<DateTimeOffset?> GetOldestCreatedAtSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task AddAsync(Swipe swipe, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task RemoveAllByUserAsync(Guid fromUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task RemoveAllInvolvingUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            RemovedInvolvingUserId = userId;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");
    }

    private sealed class FakeMatchRepository : IMatchRepository
    {
        public Guid? RemovedForUserId { get; private set; }

        public Task AddAsync(Match match, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public void Remove(Match match) => throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<int> CountNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<Match?> GetByIdForUserBasicAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task RemoveAllForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            RemovedForUserId = userId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePhotoRepository : IPhotoRepository
    {
        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task<List<Photo>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(new List<Photo>());

        public Task AddAsync(Photo photo, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public void Remove(Photo photo) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах аутентификации.");
    }

    private sealed class FakeSparkTransactionRepository : ISparkTransactionRepository
    {
        public List<SparkTransaction> Added { get; } = [];

        public Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken)
        {
            Added.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<SparkTransaction>, int)>(([], 0));

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact(DisplayName = "КОГДА новый пользователь авторизуется по реферальной ссылке ТОГДА заводится Referral со статусом Registered (T-20.1)")]
    public async Task Handle_attributes_a_referral_for_a_new_user_with_a_valid_start_param()
    {
        var referrer = new User { Id = Guid.NewGuid(), TelegramId = 7, Status = UserStatus.Active, Name = "Referrer" };
        var repository = new FakeUserRepository(referrer);
        var referralRepository = new FakeReferralRepository();
        var handler = CreateHandler(repository, referralRepository);
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
        var handler = CreateHandler(repository, referralRepository);

        await handler.Handle(new AuthenticateTelegramUserCommand(MakeInitData(startParam: "ref_not-a-valid-code")), CancellationToken.None);

        Assert.Empty(referralRepository.Referrals);
    }

    [Fact(DisplayName = "КОГДА реферер по коду не найден ТОГДА Referral не создаётся")]
    public async Task Handle_ignores_a_start_param_for_an_unknown_referrer()
    {
        var repository = new FakeUserRepository();
        var referralRepository = new FakeReferralRepository();
        var handler = CreateHandler(repository, referralRepository);
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
