using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;

namespace Blizka.UnitTests.Sparks;

public sealed class SparksServiceTests
{
    [Fact(DisplayName = "КОГДА баланса хватает ТОГДА SpendAsync списывает зорки и добавляет отрицательную транзакцию")]
    public async Task SpendAsync_deducts_the_balance_and_records_a_negative_transaction()
    {
        var user = CreateUser(sparksBalance: 10);
        var referenceId = Guid.NewGuid();
        var sparkRepository = new FakeSparkTransactionRepository();
        var service = CreateService(sparkRepository, user);

        await service.SpendAsync(user, 3, SparkTransactionType.Superlike, referenceId, CancellationToken.None);

        Assert.Equal(7, user.SparksBalance);
        var transaction = Assert.Single(sparkRepository.Transactions);
        Assert.Equal(-3, transaction.Amount);
        Assert.Equal(SparkTransactionType.Superlike, transaction.Type);
        Assert.Equal(referenceId, transaction.ReferenceId);
        Assert.Equal(7, transaction.BalanceAfter);
    }

    [Fact(DisplayName = "КОГДА баланса не хватает ТОГДА SpendAsync выбрасывает InsufficientSparksException и не меняет баланс")]
    public async Task SpendAsync_throws_when_the_balance_is_insufficient()
    {
        var user = CreateUser(sparksBalance: 2);
        var sparkRepository = new FakeSparkTransactionRepository();
        var service = CreateService(sparkRepository, user);

        await Assert.ThrowsAsync<InsufficientSparksException>(
            () => service.SpendAsync(user, 3, SparkTransactionType.Superlike, referenceId: null, CancellationToken.None));

        Assert.Equal(2, user.SparksBalance);
        Assert.Empty(sparkRepository.Transactions);
    }

    [Fact(DisplayName = "КОГДА вызывается RefundAsync ТОГДА баланс возвращается и добавляется транзакция типа Refund")]
    public async Task RefundAsync_credits_the_balance_and_records_a_refund_transaction()
    {
        var user = CreateUser(sparksBalance: 4);
        var referenceId = Guid.NewGuid();
        var sparkRepository = new FakeSparkTransactionRepository();
        var service = CreateService(sparkRepository, user);

        await service.RefundAsync(user, 5, referenceId, CancellationToken.None);

        Assert.Equal(9, user.SparksBalance);
        var transaction = Assert.Single(sparkRepository.Transactions);
        Assert.Equal(5, transaction.Amount);
        Assert.Equal(SparkTransactionType.Refund, transaction.Type);
        Assert.Equal(referenceId, transaction.ReferenceId);
    }

    [Fact(DisplayName = "КОГДА вызывается AwardAsync ТОГДА баланс увеличивается и добавляется транзакция указанного типа")]
    public async Task AwardAsync_credits_the_balance_and_records_a_transaction_of_the_given_type()
    {
        var user = CreateUser(sparksBalance: 0);
        var sparkRepository = new FakeSparkTransactionRepository();
        var service = CreateService(sparkRepository, user);

        await service.AwardAsync(user, 50, SparkTransactionType.RegistrationBonus, referenceId: null, CancellationToken.None);

        Assert.Equal(50, user.SparksBalance);
        var transaction = Assert.Single(sparkRepository.Transactions);
        Assert.Equal(50, transaction.Amount);
        Assert.Equal(SparkTransactionType.RegistrationBonus, transaction.Type);
        Assert.Null(transaction.ReferenceId);
        Assert.Equal(50, transaction.BalanceAfter);
    }

    [Fact(DisplayName = "КОГДА вызывается GetBalanceAsync ТОГДА возвращается SparksBalance пользователя из репозитория")]
    public async Task GetBalanceAsync_returns_the_users_balance()
    {
        var user = CreateUser(sparksBalance: 42);
        var service = CreateService(new FakeSparkTransactionRepository(), user);

        var balance = await service.GetBalanceAsync(user.Id, CancellationToken.None);

        Assert.Equal(42, balance);
    }

    [Fact(DisplayName = "КОГДА пользователь не найден ТОГДА GetBalanceAsync выбрасывает InvalidOperationException")]
    public async Task GetBalanceAsync_throws_when_the_user_is_not_found()
    {
        var service = CreateService(new FakeSparkTransactionRepository(), user: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetBalanceAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА вызывается GetHistoryAsync ТОГДА параметры передаются в репозиторий и результат возвращается как есть")]
    public async Task GetHistoryAsync_delegates_to_the_repository()
    {
        var user = CreateUser(sparksBalance: 0);
        var expectedTransaction = new SparkTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Amount = 5,
            Type = SparkTransactionType.Referral,
            BalanceAfter = 5,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var sparkRepository = new FakeSparkTransactionRepository { HistoryResult = ([expectedTransaction], 1) };
        var service = CreateService(sparkRepository, user);

        var (items, totalCount) = await service.GetHistoryAsync(user.Id, page: 2, pageSize: 10, CancellationToken.None);

        Assert.Equal(user.Id, sparkRepository.LastHistoryUserId);
        Assert.Equal(2, sparkRepository.LastHistoryPage);
        Assert.Equal(10, sparkRepository.LastHistoryPageSize);
        Assert.Same(expectedTransaction, Assert.Single(items));
        Assert.Equal(1, totalCount);
    }

    private static SparksService CreateService(FakeSparkTransactionRepository sparkRepository, User? user) =>
        new(sparkRepository, new FakeUserRepository(user));

    private static User CreateUser(int sparksBalance) => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = "Me",
        BirthDate = new DateOnly(1995, 1, 1),
        Gender = Gender.Female,
        Locale = "ru",
        SparksBalance = sparksBalance,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeSparkTransactionRepository : ISparkTransactionRepository
    {
        public List<SparkTransaction> Transactions { get; } = [];

        public (IReadOnlyList<SparkTransaction> Items, int TotalCount) HistoryResult { get; set; } = ([], 0);

        public Guid LastHistoryUserId { get; private set; }

        public int LastHistoryPage { get; private set; }

        public int LastHistoryPageSize { get; private set; }

        public Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken)
        {
            LastHistoryUserId = userId;
            LastHistoryPage = page;
            LastHistoryPageSize = pageSize;
            return Task.FromResult(HistoryResult);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах SparksService.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах SparksService.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(user is not null && user.Id == id ? user : null);

        public Task AddAsync(User newUser, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах SparksService.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах SparksService.");
    }
}
