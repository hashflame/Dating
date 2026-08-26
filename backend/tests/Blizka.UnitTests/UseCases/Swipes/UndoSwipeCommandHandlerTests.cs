using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Swipes;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.Swipes;

public sealed class UndoSwipeCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА дневной лимит отмен исчерпан ТОГДА выбрасывается UndoLimitExceededException")]
    public async Task Handle_throws_when_the_daily_undo_limit_is_reached()
    {
        var user = CreateUser();
        var handler = CreateHandler(out var swipeRepository, out _, users: [user]);
        swipeRepository.UndoneCount = 3;

        await Assert.ThrowsAsync<UndoLimitExceededException>(
            () => handler.Handle(new UndoSwipeCommand(user.Id), CancellationToken.None));
        Assert.False(swipeRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА у пользователя нет активного свайпа ТОГДА выбрасывается NothingToUndoException")]
    public async Task Handle_throws_when_there_is_no_active_swipe()
    {
        var user = CreateUser();
        var handler = CreateHandler(out var swipeRepository, out _, users: [user]);
        swipeRepository.LastActiveSwipe = null;

        await Assert.ThrowsAsync<NothingToUndoException>(
            () => handler.Handle(new UndoSwipeCommand(user.Id), CancellationToken.None));
        Assert.False(swipeRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА отменяется дизлайк ТОГДА UndoneAt проставлен, мэтч не ищется и зорки не возвращаются")]
    public async Task Handle_undoes_a_dislike_without_touching_matches_or_sparks()
    {
        var user = CreateUser(sparksBalance: 7);
        var targetId = Guid.NewGuid();
        var swipe = CreateSwipe(user.Id, targetId, SwipeType.Dislike);
        var handler = CreateHandler(out var swipeRepository, out var matchRepository, users: [user]);
        swipeRepository.LastActiveSwipe = swipe;

        var result = await handler.Handle(new UndoSwipeCommand(user.Id), CancellationToken.None);

        Assert.NotNull(swipe.UndoneAt);
        Assert.Equal(targetId, result.UserId);
        Assert.Equal(SwipeType.Dislike, result.Type);
        Assert.Equal(7, result.SparksBalance);
        Assert.False(matchRepository.GetByUsersCalled);
        Assert.Null(matchRepository.RemovedMatch);
        Assert.True(swipeRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА отменяется лайк, приведший к мэтчу с неоткрытым контактом ТОГДА мэтч удаляется")]
    public async Task Handle_removes_the_match_when_the_undone_like_created_one_and_contact_is_not_unlocked()
    {
        var user = CreateUser();
        var targetId = Guid.NewGuid();
        var swipe = CreateSwipe(user.Id, targetId, SwipeType.Like);
        var handler = CreateHandler(out var swipeRepository, out var matchRepository, users: [user]);
        swipeRepository.LastActiveSwipe = swipe;
        matchRepository.MatchForUsers = new Match { Id = Guid.NewGuid(), ContactUnlockedAt = null };

        await handler.Handle(new UndoSwipeCommand(user.Id), CancellationToken.None);

        Assert.NotNull(matchRepository.RemovedMatch);
    }

    [Fact(DisplayName = "КОГДА отменяется лайк, но контакт по мэтчу уже открыт ТОГДА мэтч не удаляется")]
    public async Task Handle_keeps_the_match_when_contact_is_already_unlocked()
    {
        var user = CreateUser();
        var targetId = Guid.NewGuid();
        var swipe = CreateSwipe(user.Id, targetId, SwipeType.Like);
        var handler = CreateHandler(out var swipeRepository, out var matchRepository, users: [user]);
        swipeRepository.LastActiveSwipe = swipe;
        matchRepository.MatchForUsers = new Match { Id = Guid.NewGuid(), ContactUnlockedAt = DateTimeOffset.UtcNow };

        await handler.Handle(new UndoSwipeCommand(user.Id), CancellationToken.None);

        Assert.Null(matchRepository.RemovedMatch);
    }

    [Fact(DisplayName = "КОГДА отменяется суперлайк ТОГДА зорки возвращены и баланс в ответе увеличен")]
    public async Task Handle_refunds_sparks_when_undoing_a_superlike()
    {
        var user = CreateUser(sparksBalance: 3);
        var targetId = Guid.NewGuid();
        var swipe = CreateSwipe(user.Id, targetId, SwipeType.Superlike);
        var handler = CreateHandler(out var swipeRepository, out _, users: [user], superlikeCost: 5);
        swipeRepository.LastActiveSwipe = swipe;

        var result = await handler.Handle(new UndoSwipeCommand(user.Id), CancellationToken.None);

        Assert.Equal(8, user.SparksBalance);
        Assert.Equal(8, result.SparksBalance);
    }

    [Fact(DisplayName = "КОГДА SaveChangesAsync падает на конкурентном сохранении ТОГДА выбрасывается SwipeConflictException")]
    public async Task Handle_translates_a_concurrent_save_race_into_SwipeConflictException()
    {
        var user = CreateUser();
        var targetId = Guid.NewGuid();
        var swipe = CreateSwipe(user.Id, targetId, SwipeType.Like);
        var handler = CreateHandler(out var swipeRepository, out _, users: [user]);
        swipeRepository.LastActiveSwipe = swipe;
        swipeRepository.SaveChangesFailsWith = new ConcurrentUserUpdateException(
            user.Id, new InvalidOperationException("simulated concurrency conflict"));

        var exception = await Assert.ThrowsAsync<SwipeConflictException>(
            () => handler.Handle(new UndoSwipeCommand(user.Id), CancellationToken.None));
        Assert.Equal(user.Id, exception.FromUserId);
    }

    [Fact(DisplayName = "КОГДА отмена успешна ТОГДА undosRemaining учитывает уже использованные сегодня отмены")]
    public async Task Handle_returns_the_correct_undos_remaining()
    {
        var user = CreateUser();
        var targetId = Guid.NewGuid();
        var swipe = CreateSwipe(user.Id, targetId, SwipeType.Like);
        var handler = CreateHandler(out var swipeRepository, out _, users: [user]);
        swipeRepository.LastActiveSwipe = swipe;
        swipeRepository.UndoneCount = 1;

        var result = await handler.Handle(new UndoSwipeCommand(user.Id), CancellationToken.None);

        Assert.Equal(1, result.UndosRemaining);
    }

    private static UndoSwipeCommandHandler CreateHandler(
        out FakeSwipeRepository swipeRepository, out FakeMatchRepository matchRepository,
        IReadOnlyList<User> users, int superlikeCost = 5)
    {
        var userRepository = new FakeUserRepository(users);
        swipeRepository = new FakeSwipeRepository();
        matchRepository = new FakeMatchRepository();
        var sparksService = new SparksService(new FakeSparkTransactionRepository(), userRepository);
        var options = Options.Create(new SparksOptions { SuperlikeCost = superlikeCost });

        return new UndoSwipeCommandHandler(userRepository, swipeRepository, matchRepository, sparksService, options);
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

    private static Swipe CreateSwipe(Guid fromUserId, Guid toUserId, SwipeType type) => new()
    {
        Id = Guid.NewGuid(),
        FromUserId = fromUserId,
        ToUserId = toUserId,
        Type = type,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeUserRepository(IReadOnlyList<User> users) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");
    }

    private sealed class FakeSwipeRepository : ISwipeRepository
    {
        public Swipe? LastActiveSwipe { get; set; }

        public int UndoneCount { get; set; }

        public bool SaveChangesCalled { get; private set; }

        /// <summary>Когда задано, следующий SaveChangesAsync симулирует гонку сохранения (двойная отмена/конкурентный запрос) вместо реального успешного сохранения.</summary>
        public Exception? SaveChangesFailsWith { get; set; }

        public Task<bool> ExistsActiveAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task<bool> HasActiveMutualLikeAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task<Swipe?> GetLastActiveAsync(Guid fromUserId, CancellationToken cancellationToken) =>
            Task.FromResult(LastActiveSwipe);

        public Task<int> CountUndoneSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            Task.FromResult(UndoneCount);

        public Task<int> CountSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task<DateTimeOffset?> GetOldestCreatedAtSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task AddAsync(Swipe swipe, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task RemoveAllInvolvingUserAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("RemoveAllInvolvingUserAsync не используется в этом тесте.");

        public Task RemoveAllByUserAsync(Guid fromUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
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
        public Match? MatchForUsers { get; set; }

        public Match? RemovedMatch { get; private set; }

        public bool GetByUsersCalled { get; private set; }

        public Task AddAsync(Match match, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken)
        {
            GetByUsersCalled = true;
            return Task.FromResult(MatchForUsers);
        }

        public void Remove(Match match) => RemovedMatch = match;

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task<int> CountNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task<Match?> GetByIdForUserBasicAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");

        public Task RemoveAllForUserAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("RemoveAllForUserAsync не используется в этом тесте.");

        public Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах отмены свайпа.");
    }

    private sealed class FakeSparkTransactionRepository : ISparkTransactionRepository
    {
        public List<SparkTransaction> Transactions { get; } = [];

        public Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<SparkTransaction>, int)>(([], 0));

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
