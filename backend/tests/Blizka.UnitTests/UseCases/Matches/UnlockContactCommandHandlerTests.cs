using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.Subscriptions;
using Blizka.App.UseCases.Matches;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.Matches;

public sealed class UnlockContactCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА контакт ещё не открыт и баланса хватает ТОГДА зорки списываются и контакт открывается")]
    public async Task Handle_spends_sparks_and_unlocks_the_contact()
    {
        var currentUser = CreateUser(sparksBalance: 5);
        var other = CreateUser(name: "Anna", telegramUsername: "anna_k");
        var match = CreateMatch(currentUser, other);
        var handler = CreateHandler(out var matchRepository, match);

        var result = await handler.Handle(new UnlockContactCommand(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Equal("anna_k", result.TelegramUsername);
        Assert.Equal("https://t.me/anna_k", result.DeepLink);
        Assert.Equal(1, result.SparksSpent);
        Assert.Equal(4, result.SparksBalance);
        Assert.Equal(4, currentUser.SparksBalance);
        Assert.NotNull(match.ContactUnlockedAt);
        Assert.Equal(currentUser.Id, match.ContactUnlockedByUserId);
        Assert.True(matchRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА у второго участника нет публичного username в Telegram ТОГДА telegramUsername и deepLink null")]
    public async Task Handle_returns_null_contact_when_the_other_user_has_no_telegram_username()
    {
        var currentUser = CreateUser(sparksBalance: 5);
        var other = CreateUser(name: "Anna", telegramUsername: null);
        var match = CreateMatch(currentUser, other);
        var handler = CreateHandler(out _, match);

        var result = await handler.Handle(new UnlockContactCommand(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Null(result.TelegramUsername);
        Assert.Null(result.DeepLink);
        Assert.Equal(1, result.SparksSpent);
    }

    [Fact(DisplayName = "КОГДА баланса не хватает ТОГДА выбрасывается InsufficientSparksException, контакт остаётся закрыт")]
    public async Task Handle_throws_when_the_balance_is_insufficient()
    {
        var currentUser = CreateUser(sparksBalance: 0);
        var other = CreateUser(name: "Anna", telegramUsername: "anna_k");
        var match = CreateMatch(currentUser, other);
        var handler = CreateHandler(out var matchRepository, match);

        await Assert.ThrowsAsync<InsufficientSparksException>(
            () => handler.Handle(new UnlockContactCommand(match.Id, currentUser.Id), CancellationToken.None));
        Assert.Null(match.ContactUnlockedAt);
        Assert.False(matchRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА контакт уже открыт (кем угодно из пары) ТОГДА повторный вызов идемпотентен — зорки не списываются")]
    public async Task Handle_is_idempotent_when_the_contact_is_already_unlocked()
    {
        var currentUser = CreateUser(sparksBalance: 5);
        var other = CreateUser(name: "Anna", telegramUsername: "anna_k");
        var match = CreateMatch(currentUser, other);
        match.ContactUnlockedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        match.ContactUnlockedByUserId = other.Id;
        var handler = CreateHandler(out var matchRepository, match);

        var result = await handler.Handle(new UnlockContactCommand(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Equal(0, result.SparksSpent);
        Assert.Equal(5, result.SparksBalance);
        Assert.Equal(5, currentUser.SparksBalance);
        Assert.False(matchRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА у пользователя подписка «Безлимит» ТОГДА контакт открывается бесплатно, без списания и без записи в SparkTransaction")]
    public async Task Handle_unlocks_for_free_when_the_user_has_an_unlimited_subscription()
    {
        var currentUser = CreateUser(sparksBalance: 5);
        var other = CreateUser(name: "Anna", telegramUsername: "anna_k");
        var match = CreateMatch(currentUser, other);
        var handler = CreateHandler(out var matchRepository, match, subscriptionChecker: new FakeSubscriptionChecker(hasUnlimitedContactUnlocks: true));

        var result = await handler.Handle(new UnlockContactCommand(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Equal(0, result.SparksSpent);
        Assert.Equal(5, result.SparksBalance);
        Assert.Equal(5, currentUser.SparksBalance);
        Assert.NotNull(match.ContactUnlockedAt);
        Assert.True(matchRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА мэтча с таким id нет для этого пользователя ТОГДА выбрасывается MatchNotFoundException")]
    public async Task Handle_throws_when_the_match_is_not_found_for_the_requesting_user()
    {
        var handler = CreateHandler(out _, match: null);

        await Assert.ThrowsAsync<MatchNotFoundException>(
            () => handler.Handle(new UnlockContactCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА SaveChangesAsync падает на конкурентном сохранении ТОГДА выбрасывается ContactUnlockConflictException")]
    public async Task Handle_translates_a_concurrent_save_race_into_ContactUnlockConflictException()
    {
        var currentUser = CreateUser(sparksBalance: 5);
        var other = CreateUser(name: "Anna", telegramUsername: "anna_k");
        var match = CreateMatch(currentUser, other);
        var handler = CreateHandler(out var matchRepository, match);
        matchRepository.SaveChangesFailsWith = new ConcurrentUserUpdateException(
            currentUser.Id, new InvalidOperationException("simulated concurrency conflict"));

        var exception = await Assert.ThrowsAsync<ContactUnlockConflictException>(
            () => handler.Handle(new UnlockContactCommand(match.Id, currentUser.Id), CancellationToken.None));
        Assert.Equal(match.Id, exception.MatchId);
    }

    private static UnlockContactCommandHandler CreateHandler(
        out FakeMatchRepository matchRepository, Match? match, ISubscriptionChecker? subscriptionChecker = null)
    {
        matchRepository = new FakeMatchRepository { ById = match };
        var sparksService = new SparksService(new FakeSparkTransactionRepository());
        var options = Options.Create(new SparksOptions { ContactUnlockCost = 1 });

        return new UnlockContactCommandHandler(matchRepository, sparksService, options, subscriptionChecker);
    }

    private static User CreateUser(string name = "Me", int sparksBalance = 0, string? telegramUsername = null) => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = name,
        BirthDate = new DateOnly(1995, 1, 1),
        Gender = Gender.Female,
        Locale = "ru",
        SparksBalance = sparksBalance,
        TelegramUsername = telegramUsername,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static Match CreateMatch(User currentUser, User other)
    {
        var (user1, user2) = currentUser.Id.CompareTo(other.Id) < 0 ? (currentUser, other) : (other, currentUser);
        return new Match
        {
            Id = Guid.NewGuid(),
            User1Id = user1.Id,
            User1 = user1,
            User2Id = user2.Id,
            User2 = user2,
            Status = MatchStatus.Active,
            MatchedAt = DateTimeOffset.UtcNow,
        };
    }

    private sealed class FakeMatchRepository : IMatchRepository
    {
        public Match? ById { get; set; }

        public bool SaveChangesCalled { get; private set; }

        public Exception? SaveChangesFailsWith { get; set; }

        public Task AddAsync(Match match, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах открытия контакта.");

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах открытия контакта.");

        public void Remove(Match match) =>
            throw new NotSupportedException("Не используется в тестах открытия контакта.");

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах открытия контакта.");

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах открытия контакта.");

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах открытия контакта.");

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах открытия контакта.");

        public Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken)
        {
            var found = ById is not null && ById.Id == matchId && (ById.User1Id == userId || ById.User2Id == userId)
                ? ById
                : null;
            return Task.FromResult(found);
        }

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

    private sealed class FakeSubscriptionChecker(bool hasUnlimitedContactUnlocks) : ISubscriptionChecker
    {
        public Task<bool> HasUnlimitedSwipesAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах открытия контакта.");

        public Task<bool> HasUnlimitedContactUnlocksAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(hasUnlimitedContactUnlocks);
    }
}
