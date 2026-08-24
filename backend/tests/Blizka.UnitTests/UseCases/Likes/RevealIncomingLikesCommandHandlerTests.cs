using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Likes;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.Likes;

public sealed class RevealIncomingLikesCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА список ещё не разблокирован ТОГДА зорки списываются, флаг выставляется и возвращается полный список")]
    public async Task Handle_spends_sparks_and_reveals_the_list()
    {
        var user = CreateUser(sparksBalance: 15, likesRevealed: false);
        var liker = CreateUser(name: "Anna");
        var handler = CreateHandler(out var userRepository, out var likesRepository, users: [user]);
        likesRepository.Incoming = [new LikeEntry(liker, DateTimeOffset.UtcNow)];

        var result = await handler.Handle(new RevealIncomingLikesCommand(user.Id), CancellationToken.None);

        Assert.Equal(10, result.SparksSpent);
        Assert.Equal(5, result.SparksBalance);
        Assert.Equal(5, user.SparksBalance);
        Assert.True(user.LikesRevealed);
        Assert.Single(result.Users);
        Assert.True(userRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА баланса не хватает ТОГДА выбрасывается InsufficientSparksException, флаг не выставляется")]
    public async Task Handle_throws_when_the_balance_is_insufficient()
    {
        var user = CreateUser(sparksBalance: 3, likesRevealed: false);
        var handler = CreateHandler(out var userRepository, out _, users: [user]);

        await Assert.ThrowsAsync<InsufficientSparksException>(
            () => handler.Handle(new RevealIncomingLikesCommand(user.Id), CancellationToken.None));
        Assert.False(user.LikesRevealed);
        Assert.False(userRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА список уже разблокирован ТОГДА повторный вызов идемпотентен — зорки не списываются")]
    public async Task Handle_is_idempotent_when_already_revealed()
    {
        var user = CreateUser(sparksBalance: 15, likesRevealed: true);
        var liker = CreateUser(name: "Anna");
        var handler = CreateHandler(out var userRepository, out var likesRepository, users: [user]);
        likesRepository.Incoming = [new LikeEntry(liker, DateTimeOffset.UtcNow)];

        var result = await handler.Handle(new RevealIncomingLikesCommand(user.Id), CancellationToken.None);

        Assert.Equal(0, result.SparksSpent);
        Assert.Equal(15, result.SparksBalance);
        Assert.Equal(15, user.SparksBalance);
        Assert.Single(result.Users);
        Assert.False(userRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА SaveChangesAsync падает на конкурентном сохранении ТОГДА выбрасывается LikesRevealConflictException")]
    public async Task Handle_translates_a_concurrent_save_race_into_LikesRevealConflictException()
    {
        var user = CreateUser(sparksBalance: 15, likesRevealed: false);
        var handler = CreateHandler(out var userRepository, out _, users: [user]);
        userRepository.SaveChangesFailsWith = new ConcurrentUserUpdateException(
            user.Id, new InvalidOperationException("simulated concurrency conflict"));

        var exception = await Assert.ThrowsAsync<LikesRevealConflictException>(
            () => handler.Handle(new RevealIncomingLikesCommand(user.Id), CancellationToken.None));
        Assert.Equal(user.Id, exception.UserId);
    }

    private static RevealIncomingLikesCommandHandler CreateHandler(
        out FakeUserRepository userRepository, out FakeLikesRepository likesRepository, IReadOnlyList<User> users)
    {
        userRepository = new FakeUserRepository(users);
        likesRepository = new FakeLikesRepository();
        var sparksService = new SparksService(new FakeSparkTransactionRepository());
        var options = Options.Create(new SparksOptions { LikesRevealCost = 10 });

        return new RevealIncomingLikesCommandHandler(userRepository, likesRepository, sparksService, options);
    }

    private static User CreateUser(string name = "User", int sparksBalance = 0, bool likesRevealed = false) => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = name,
        BirthDate = new DateOnly(1995, 1, 1),
        Gender = Gender.Female,
        Locale = "ru",
        SparksBalance = sparksBalance,
        LikesRevealed = likesRevealed,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeUserRepository(IReadOnlyList<User> users) : IUserRepository
    {
        public bool SaveChangesCalled { get; private set; }

        public Exception? SaveChangesFailsWith { get; set; }

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах разблокировки лайков.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах разблокировки лайков.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах разблокировки лайков.");

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

    private sealed class FakeLikesRepository : ILikesRepository
    {
        public IReadOnlyList<LikeEntry> Incoming { get; set; } = [];

        public Task<int> CountIncomingAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах разблокировки лайков.");

        public Task<IReadOnlyList<LikeEntry>> GetIncomingPreviewAsync(Guid userId, int limit, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах разблокировки лайков.");

        public Task<IReadOnlyList<LikeEntry>> GetIncomingAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Incoming);

        public Task<IReadOnlyList<LikeEntry>> GetOutgoingAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах разблокировки лайков.");
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
