using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.Subscriptions;
using Blizka.App.UseCases.Swipes;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.Swipes;

public sealed class SwipeCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА цель свайпа не найдена ТОГДА выбрасывается SwipeTargetNotFoundException")]
    public async Task Handle_throws_when_the_target_user_does_not_exist()
    {
        var fromUser = CreateUser();
        var handler = CreateHandler(out var swipeRepository, users: [fromUser]);

        await Assert.ThrowsAsync<SwipeTargetNotFoundException>(
            () => handler.Handle(new SwipeCommand(fromUser.Id, Guid.NewGuid(), SwipeType.Like), CancellationToken.None));
        Assert.Empty(swipeRepository.AddedSwipes);
    }

    [Fact(DisplayName = "КОГДА пользователь свайпает самого себя ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_when_swiping_self()
    {
        var user = CreateUser();
        var handler = CreateHandler(out _, users: [user]);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new SwipeCommand(user.Id, user.Id, SwipeType.Like), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА пара уже активно свайпнута ТОГДА выбрасывается AlreadySwipedException")]
    public async Task Handle_throws_when_the_pair_was_already_swiped()
    {
        var fromUser = CreateUser();
        var toUser = CreateUser();
        var handler = CreateHandler(out var swipeRepository, users: [fromUser, toUser]);
        swipeRepository.AlreadyActive = true;

        await Assert.ThrowsAsync<AlreadySwipedException>(
            () => handler.Handle(new SwipeCommand(fromUser.Id, toUser.Id, SwipeType.Like), CancellationToken.None));
        Assert.Empty(swipeRepository.AddedSwipes);
    }

    [Fact(DisplayName = "КОГДА SaveChangesAsync падает на гонке уникального индекса свайпа ТОГДА выбрасывается AlreadySwipedException")]
    public async Task Handle_translates_a_concurrent_swipe_creation_race_into_AlreadySwipedException()
    {
        var fromUser = CreateUser();
        var toUser = CreateUser();
        var handler = CreateHandler(out var swipeRepository, users: [fromUser, toUser]);
        swipeRepository.SaveChangesFailsWith = new ConcurrentSwipeCreationException(
            fromUser.Id, toUser.Id, new InvalidOperationException("simulated unique violation"));

        var exception = await Assert.ThrowsAsync<AlreadySwipedException>(
            () => handler.Handle(new SwipeCommand(fromUser.Id, toUser.Id, SwipeType.Like), CancellationToken.None));
        Assert.Equal(toUser.Id, exception.ToUserId);
    }

    [Fact(DisplayName = "КОГДА SaveChangesAsync падает на конкурентном обновлении баланса ТОГДА выбрасывается SwipeConflictException")]
    public async Task Handle_translates_a_concurrent_user_update_race_into_SwipeConflictException()
    {
        var fromUser = CreateUser(sparksBalance: 10);
        var toUser = CreateUser();
        var handler = CreateHandler(out var swipeRepository, users: [fromUser, toUser], superlikeCost: 5);
        swipeRepository.SaveChangesFailsWith = new ConcurrentUserUpdateException(
            fromUser.Id, new InvalidOperationException("simulated concurrency conflict"));

        var exception = await Assert.ThrowsAsync<SwipeConflictException>(
            () => handler.Handle(new SwipeCommand(fromUser.Id, toUser.Id, SwipeType.Superlike), CancellationToken.None));
        Assert.Equal(fromUser.Id, exception.FromUserId);
    }

    [Fact(DisplayName = "КОГДА лайк без встречного лайка ТОГДА свайп создан, мэтча нет")]
    public async Task Handle_records_a_like_without_a_match_when_there_is_no_mutual_like()
    {
        var fromUser = CreateUser();
        var toUser = CreateUser();
        var handler = CreateHandler(out var swipeRepository, out var matchRepository, users: [fromUser, toUser]);

        var result = await handler.Handle(new SwipeCommand(fromUser.Id, toUser.Id, SwipeType.Like), CancellationToken.None);

        Assert.False(result.IsMatch);
        Assert.Null(result.Match);
        var swipe = Assert.Single(swipeRepository.AddedSwipes);
        Assert.Equal(SwipeType.Like, swipe.Type);
        Assert.Empty(matchRepository.AddedMatches);
    }

    [Fact(DisplayName = "КОГДА лайк при наличии встречного лайка ТОГДА создаётся мэтч с каноничным порядком пары и тремя icebreakers")]
    public async Task Handle_creates_a_match_with_canonical_pair_order_on_mutual_like()
    {
        var fromUser = CreateUser();
        var toUser = CreateUser(name: "Анна");
        var handler = CreateHandler(out var swipeRepository, out var matchRepository, users: [fromUser, toUser]);
        swipeRepository.HasMutualLike = true;

        var result = await handler.Handle(new SwipeCommand(fromUser.Id, toUser.Id, SwipeType.Like), CancellationToken.None);

        Assert.True(result.IsMatch);
        Assert.NotNull(result.Match);
        Assert.Equal(toUser.Id, result.Match.UserId);
        Assert.Equal("Анна", result.Match.Name);
        Assert.Equal(3, result.Match.Icebreakers.Count);
        var match = Assert.Single(matchRepository.AddedMatches);
        var expectedUser1 = fromUser.Id.CompareTo(toUser.Id) < 0 ? fromUser.Id : toUser.Id;
        var expectedUser2 = fromUser.Id.CompareTo(toUser.Id) < 0 ? toUser.Id : fromUser.Id;
        Assert.Equal(expectedUser1, match.User1Id);
        Assert.Equal(expectedUser2, match.User2Id);
    }

    [Fact(DisplayName = "КОГДА дизлайк при наличии встречного лайка ТОГДА мэтч не создаётся")]
    public async Task Handle_does_not_check_for_a_match_on_dislike()
    {
        var fromUser = CreateUser();
        var toUser = CreateUser();
        var handler = CreateHandler(out var swipeRepository, out var matchRepository, users: [fromUser, toUser]);
        swipeRepository.HasMutualLike = true;

        var result = await handler.Handle(new SwipeCommand(fromUser.Id, toUser.Id, SwipeType.Dislike), CancellationToken.None);

        Assert.False(result.IsMatch);
        Assert.Empty(matchRepository.AddedMatches);
    }

    [Fact(DisplayName = "КОГДА суперлайк при недостаточном балансе ТОГДА выбрасывается InsufficientSparksException, свайп не создаётся")]
    public async Task Handle_throws_when_the_superlike_cost_exceeds_the_balance()
    {
        var fromUser = CreateUser(sparksBalance: 2);
        var toUser = CreateUser();
        var handler = CreateHandler(out var swipeRepository, users: [fromUser, toUser], superlikeCost: 5);

        await Assert.ThrowsAsync<InsufficientSparksException>(
            () => handler.Handle(new SwipeCommand(fromUser.Id, toUser.Id, SwipeType.Superlike), CancellationToken.None));
        Assert.Empty(swipeRepository.AddedSwipes);
    }

    [Fact(DisplayName = "КОГДА суперлайк с достаточным балансом ТОГДА зорки списаны и баланс в ответе уменьшен")]
    public async Task Handle_spends_sparks_on_superlike()
    {
        var fromUser = CreateUser(sparksBalance: 10);
        var toUser = CreateUser();
        var handler = CreateHandler(out var swipeRepository, users: [fromUser, toUser], superlikeCost: 5);

        var result = await handler.Handle(new SwipeCommand(fromUser.Id, toUser.Id, SwipeType.Superlike), CancellationToken.None);

        Assert.Equal(5, result.SparksBalance);
        Assert.Equal(5, fromUser.SparksBalance);
        var swipe = Assert.Single(swipeRepository.AddedSwipes);
        Assert.Equal(SwipeType.Superlike, swipe.Type);
    }

    [Fact(DisplayName = "КОГДА пользователь уже сделал 50 свайпов за 24 часа ТОГДА выбрасывается DailySwipeLimitExceededException (spec 002, B3)")]
    public async Task Handle_throws_when_the_daily_swipe_limit_is_reached()
    {
        var fromUser = CreateUser();
        var toUser = CreateUser();
        var handler = CreateHandler(out var swipeRepository, users: [fromUser, toUser]);
        swipeRepository.SwipesUsedToday = SwipeLimits.DailyLimit;
        var oldest = DateTimeOffset.UtcNow.AddHours(-1);
        swipeRepository.OldestSwipeCreatedAt = oldest;

        var exception = await Assert.ThrowsAsync<DailySwipeLimitExceededException>(
            () => handler.Handle(new SwipeCommand(fromUser.Id, toUser.Id, SwipeType.Like), CancellationToken.None));

        Assert.Equal(oldest.AddHours(24), exception.ResetAt);
        Assert.Empty(swipeRepository.AddedSwipes);
    }

    [Fact(DisplayName = "КОГДА у пользователя лимит исчерпан, но есть безлимитная подписка ТОГДА лимит не применяется (точка расширения T-8.3)")]
    public async Task Handle_bypasses_the_daily_limit_for_unlimited_subscribers()
    {
        var fromUser = CreateUser();
        var toUser = CreateUser();
        var handler = CreateHandler(out var swipeRepository, users: [fromUser, toUser], subscriptionChecker: new FakeSubscriptionChecker(hasUnlimitedSwipes: true));
        swipeRepository.SwipesUsedToday = SwipeLimits.DailyLimit;

        var result = await handler.Handle(new SwipeCommand(fromUser.Id, toUser.Id, SwipeType.Like), CancellationToken.None);

        Assert.False(result.IsMatch);
        Assert.Single(swipeRepository.AddedSwipes);
    }

    private static SwipeCommandHandler CreateHandler(
        out FakeSwipeRepository swipeRepository, IReadOnlyList<User> users, int superlikeCost = 5, ISubscriptionChecker? subscriptionChecker = null) =>
        CreateHandler(out swipeRepository, out _, users, superlikeCost, subscriptionChecker);

    private static SwipeCommandHandler CreateHandler(
        out FakeSwipeRepository swipeRepository, out FakeMatchRepository matchRepository,
        IReadOnlyList<User> users, int superlikeCost = 5, ISubscriptionChecker? subscriptionChecker = null)
    {
        var userRepository = new FakeUserRepository(users);
        swipeRepository = new FakeSwipeRepository();
        matchRepository = new FakeMatchRepository();
        var sparksService = new SparksService(new FakeSparkTransactionRepository());
        var options = Options.Create(new SparksOptions { SuperlikeCost = superlikeCost });

        return new SwipeCommandHandler(
            userRepository, swipeRepository, matchRepository, sparksService, options, new SwipeCommandValidator(), subscriptionChecker);
    }

    private sealed class FakeSubscriptionChecker(bool hasUnlimitedSwipes) : ISubscriptionChecker
    {
        public Task<bool> HasUnlimitedSwipesAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(hasUnlimitedSwipes);
    }

    private static User CreateUser(string name = "User", int sparksBalance = 0) => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = name,
        Gender = Gender.Female,
        Locale = "ru",
        SparksBalance = sparksBalance,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeUserRepository(IReadOnlyList<User> users) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");
    }

    private sealed class FakeSwipeRepository : ISwipeRepository
    {
        public List<Swipe> AddedSwipes { get; } = [];

        public bool AlreadyActive { get; set; }

        public bool HasMutualLike { get; set; }

        /// <summary>Сколько свайпов уже сделано за окно — по умолчанию 0, лимит (spec 002, B3) не задет в большинстве тестов.</summary>
        public int SwipesUsedToday { get; set; }

        public DateTimeOffset? OldestSwipeCreatedAt { get; set; }

        /// <summary>Когда задано, следующий SaveChangesAsync симулирует гонку сохранения (двойной тап/конкурентный запрос) вместо реального успешного сохранения.</summary>
        public Exception? SaveChangesFailsWith { get; set; }

        public Task<bool> ExistsActiveAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            Task.FromResult(AlreadyActive);

        public Task<bool> HasActiveMutualLikeAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            Task.FromResult(HasMutualLike);

        public Task<Swipe?> GetLastActiveAsync(Guid fromUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");

        public Task<int> CountUndoneSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");

        public Task<int> CountSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            Task.FromResult(SwipesUsedToday);

        public Task<DateTimeOffset?> GetOldestCreatedAtSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            Task.FromResult(OldestSwipeCreatedAt);

        public Task AddAsync(Swipe swipe, CancellationToken cancellationToken)
        {
            AddedSwipes.Add(swipe);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (SaveChangesFailsWith is { } exception)
            {
                SaveChangesFailsWith = null;
                throw exception;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeMatchRepository : IMatchRepository
    {
        public List<Match> AddedMatches { get; } = [];

        public Task AddAsync(Match match, CancellationToken cancellationToken)
        {
            AddedMatches.Add(match);
            return Task.CompletedTask;
        }

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");

        public void Remove(Match match) => throw new NotSupportedException("Не используется в тестах свайпа.");

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах свайпа.");
    }

    private sealed class FakeSparkTransactionRepository : ISparkTransactionRepository
    {
        public List<SparkTransaction> Transactions { get; } = [];

        public Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
