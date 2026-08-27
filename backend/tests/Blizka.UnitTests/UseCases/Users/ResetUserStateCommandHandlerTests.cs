using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Users;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.Users;

public sealed class ResetUserStateCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА пользователь сбрасывается ТОГДА чистятся свайпы, мэтчи, фото, интересы и предпочтения")]
    public async Task Handle_removes_swipes_matches_photos_interests_and_date_preferences()
    {
        var user = CreateUser();
        var interest = new Interest { Id = Guid.NewGuid(), Category = InterestCategory.Entertainment, NameRu = "Кино" };
        user.UserInterests.Add(new UserInterest { UserId = user.Id, InterestId = interest.Id, Interest = interest });
        user.UserDatePreferences.Add(new UserDatePreference { UserId = user.Id, DatePreferenceId = Guid.NewGuid() });
        var swipeRepository = new FakeSwipeRepository();
        var matchRepository = new FakeMatchRepository();
        var photoRepository = new FakePhotoRepository
        {
            Photos = [new Photo { Id = Guid.NewGuid(), UserId = user.Id, Url = "u", ThumbnailUrl = "t", MediumUrl = "m" }],
        };
        var handler = CreateHandler(user, swipeRepository, matchRepository, photoRepository, new FakeSparkTransactionRepository());

        await handler.Handle(new ResetUserStateCommand(user.Id), CancellationToken.None);

        Assert.Equal(user.Id, swipeRepository.RemovedInvolvingUserId);
        Assert.Equal(user.Id, matchRepository.RemovedForUserId);
        Assert.Single(photoRepository.Removed);
        Assert.Empty(user.UserInterests);
        Assert.Empty(user.UserDatePreferences);
    }

    [Fact(DisplayName = "КОГДА регистрационный бонус уже начислен ТОГДА баланс сбрасывается к его сумме через леджер, а не напрямую полем")]
    public async Task Handle_resets_balance_to_registration_bonus_when_already_awarded()
    {
        var user = CreateUser();
        user.RegistrationBonusAwardedAt = DateTimeOffset.UtcNow;
        user.SparksBalance = 999;
        var transactionRepository = new FakeSparkTransactionRepository();
        var handler = CreateHandler(user, new FakeSwipeRepository(), new FakeMatchRepository(), new FakePhotoRepository(), transactionRepository);

        await handler.Handle(new ResetUserStateCommand(user.Id), CancellationToken.None);

        Assert.Equal(50, user.SparksBalance);
        Assert.NotNull(user.RegistrationBonusAwardedAt);
        var transaction = Assert.Single(transactionRepository.Added);
        Assert.Equal(SparkTransactionType.DevReset, transaction.Type);
        Assert.Equal(50 - 999, transaction.Amount);
        Assert.Equal(50, transaction.BalanceAfter);
    }

    [Fact(DisplayName = "КОГДА регистрационный бонус ещё не начислен ТОГДА баланс сбрасывается в 0 через леджер")]
    public async Task Handle_resets_balance_to_zero_when_registration_bonus_was_never_awarded()
    {
        var user = CreateUser();
        user.SparksBalance = 999;
        var transactionRepository = new FakeSparkTransactionRepository();
        var handler = CreateHandler(user, new FakeSwipeRepository(), new FakeMatchRepository(), new FakePhotoRepository(), transactionRepository);

        await handler.Handle(new ResetUserStateCommand(user.Id), CancellationToken.None);

        Assert.Equal(0, user.SparksBalance);
        var transaction = Assert.Single(transactionRepository.Added);
        Assert.Equal(SparkTransactionType.DevReset, transaction.Type);
        Assert.Equal(-999, transaction.Amount);
    }

    [Fact(DisplayName = "КОГДА баланс уже равен целевому ТОГДА в леджер ничего не пишется")]
    public async Task Handle_does_not_write_a_ledger_entry_when_the_balance_is_already_at_target()
    {
        var user = CreateUser();
        user.SparksBalance = 0;
        var transactionRepository = new FakeSparkTransactionRepository();
        var handler = CreateHandler(user, new FakeSwipeRepository(), new FakeMatchRepository(), new FakePhotoRepository(), transactionRepository);

        await handler.Handle(new ResetUserStateCommand(user.Id), CancellationToken.None);

        Assert.Equal(0, user.SparksBalance);
        Assert.Empty(transactionRepository.Added);
    }

    [Fact(DisplayName = "КОГДА пороги заполненности были начислены ТОГДА они сбрасываются вместе с ProfileCompleteness до базовых 35%")]
    public async Task Handle_resets_completeness_and_threshold_flags()
    {
        var user = CreateUser();
        user.ProfileCompleteness = 100;
        user.CompletenessBonus60AwardedAt = DateTimeOffset.UtcNow;
        user.CompletenessBonus80AwardedAt = DateTimeOffset.UtcNow;
        user.CompletenessBonus100AwardedAt = DateTimeOffset.UtcNow;
        user.Bio = "Hello";
        user.IsVerified = true;
        var handler = CreateHandler(user, new FakeSwipeRepository(), new FakeMatchRepository(), new FakePhotoRepository(), new FakeSparkTransactionRepository());

        await handler.Handle(new ResetUserStateCommand(user.Id), CancellationToken.None);

        Assert.Equal(35, user.ProfileCompleteness);
        Assert.Null(user.CompletenessBonus60AwardedAt);
        Assert.Null(user.CompletenessBonus80AwardedAt);
        Assert.Null(user.CompletenessBonus100AwardedAt);
        Assert.Null(user.Bio);
        Assert.False(user.IsVerified);
    }

    [Fact(DisplayName = "КОГДА параллельный запрос уже сохранил изменения этого пользователя ТОГДА выбрасывается ProfileUpdateConflictException")]
    public async Task Handle_throws_profile_update_conflict_on_concurrent_save()
    {
        var user = CreateUser();
        var userRepository = new FakeUserRepository(user) { ThrowOnSave = true };
        var handler = new ResetUserStateCommandHandler(
            userRepository, new FakeSwipeRepository(), new FakeMatchRepository(), new FakePhotoRepository(),
            new SparksService(new FakeSparkTransactionRepository(), userRepository), CreateOptions());

        await Assert.ThrowsAsync<ProfileUpdateConflictException>(
            () => handler.Handle(new ResetUserStateCommand(user.Id), CancellationToken.None));
    }

    private static ResetUserStateCommandHandler CreateHandler(
        User user, FakeSwipeRepository swipeRepository, FakeMatchRepository matchRepository, FakePhotoRepository photoRepository,
        FakeSparkTransactionRepository transactionRepository)
    {
        var userRepository = new FakeUserRepository(user);
        return new ResetUserStateCommandHandler(
            userRepository, swipeRepository, matchRepository, photoRepository,
            new SparksService(transactionRepository, userRepository), CreateOptions());
    }

    private static IOptions<SparksOptions> CreateOptions() =>
        Options.Create(new SparksOptions { RegistrationBonusAmount = 50 });

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = 1,
        Name = "Ann",
        Status = UserStatus.Active,
    };

    private sealed class FakeUserRepository(User seed) : IUserRepository
    {
        public bool ThrowOnSave { get; init; }

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(seed.Id == id ? seed : null);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) => ThrowOnSave
            ? throw new ConcurrentUserUpdateException(seed.Id, new Exception("conflict"))
            : Task.CompletedTask;
    }

    private sealed class FakeSwipeRepository : ISwipeRepository
    {
        public Guid? RemovedInvolvingUserId { get; private set; }

        public Task<bool> ExistsActiveAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<bool> HasActiveMutualLikeAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<Swipe?> GetLastActiveAsync(Guid fromUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<int> CountUndoneSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<int> CountSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<DateTimeOffset?> GetOldestCreatedAtSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task AddAsync(Swipe swipe, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task RemoveAllByUserAsync(Guid fromUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task RemoveAllInvolvingUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            RemovedInvolvingUserId = userId;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");
    }

    private sealed class FakeMatchRepository : IMatchRepository
    {
        public Guid? RemovedForUserId { get; private set; }

        public Task AddAsync(Match match, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public void Remove(Match match) => throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<int> CountNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<Match?> GetByIdForUserBasicAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task RemoveAllForUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            RemovedForUserId = userId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePhotoRepository : IPhotoRepository
    {
        public IReadOnlyList<Photo> Photos { get; set; } = [];

        public List<Photo> Removed { get; } = [];

        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task<List<Photo>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Photos.Where(p => p.UserId == userId).ToList());

        public Task AddAsync(Photo photo, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public void Remove(Photo photo) => Removed.Add(photo);

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах сброса состояния.");
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
            throw new NotSupportedException("Не используется в тестах сброса состояния.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
