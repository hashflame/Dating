using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Sparks;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.Sparks;

public sealed class GetSparksWalletQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА запрошен кошелёк ТОГДА возвращаются баланс, страница истории и каталог начислений из конфига")]
    public async Task Handle_returns_balance_history_page_and_earn_options_from_config()
    {
        var userId = Guid.NewGuid();
        var transaction = new SparkTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = 50,
            Type = SparkTransactionType.RegistrationBonus,
            BalanceAfter = 50,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var sparksService = new FakeSparksService { Balance = 50, History = ([transaction], 1) };
        var user = new User { Id = userId, RegistrationBonusAwardedAt = DateTimeOffset.UtcNow, IsVerified = true };
        var handler = new GetSparksWalletQueryHandler(
            sparksService, new FakeUserRepository(user), CreateOptions(), new GetSparksWalletQueryValidator());

        var result = await handler.Handle(new GetSparksWalletQuery(userId, Page: 1, PageSize: 20, Locale: "ru"), CancellationToken.None);

        Assert.Equal(50, result.Balance);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        var item = Assert.Single(result.Items);
        Assert.Equal(transaction.Id, item.Id);
        Assert.Equal(SparkTransactionType.RegistrationBonus, item.Type);
        Assert.Contains(result.EarnOptions, o => o.Type == SparkTransactionType.RegistrationBonus && o.Amount == 50 && o.Completed);
        Assert.Contains(result.EarnOptions, o => o.Type == SparkTransactionType.ProfileCompletion && o.Amount == 2 && !o.Completed);
        Assert.Contains(result.EarnOptions, o => o.Type == SparkTransactionType.Verification && o.Amount == 3 && o.Completed);
        Assert.Contains(result.EarnOptions, o => o.Type == SparkTransactionType.Referral && o.Amount == 2 && !o.Completed);
        Assert.Contains(result.EarnOptions, o => o.Type == SparkTransactionType.IdeaSubmission && o.Amount == 1 && !o.Completed);
        Assert.Contains(result.EarnOptions, o => o.Type == SparkTransactionType.IdeaImplemented && o.Amount == 10 && !o.Completed);
        Assert.True(result.EarnOptions.All(o => !string.IsNullOrEmpty(o.Label)));
    }

    [Fact(DisplayName = "КОГДА профиль ещё не заполнен ТОГДА ProfileCompletion.progress/threshold отражают реальную заполненность, а не заглушку")]
    public async Task Handle_returns_real_profile_completion_progress()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, ProfileCompleteness = 45 };
        var handler = new GetSparksWalletQueryHandler(
            new FakeSparksService(), new FakeUserRepository(user), CreateOptions(), new GetSparksWalletQueryValidator());

        var result = await handler.Handle(new GetSparksWalletQuery(userId, Page: 1, PageSize: 20, Locale: "ru"), CancellationToken.None);

        var profileCompletion = result.EarnOptions.Single(o => o.Type == SparkTransactionType.ProfileCompletion);
        Assert.Equal(45, profileCompletion.Progress);
        Assert.Equal(60, profileCompletion.Threshold);
        Assert.False(profileCompletion.Completed);
    }

    [Fact(DisplayName = "КОГДА page меньше 1 ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_when_page_is_less_than_one()
    {
        var handler = new GetSparksWalletQueryHandler(
            new FakeSparksService(), new FakeUserRepository(new User()), CreateOptions(), new GetSparksWalletQueryValidator());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new GetSparksWalletQuery(Guid.NewGuid(), Page: 0, PageSize: 20, Locale: "ru"), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА pageSize вне диапазона 1-50 ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_when_page_size_is_out_of_range()
    {
        var handler = new GetSparksWalletQueryHandler(
            new FakeSparksService(), new FakeUserRepository(new User()), CreateOptions(), new GetSparksWalletQueryValidator());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new GetSparksWalletQuery(Guid.NewGuid(), Page: 1, PageSize: 51, Locale: "ru"), CancellationToken.None));
    }

    private static IOptions<SparksOptions> CreateOptions() => Options.Create(new SparksOptions
    {
        RegistrationBonusAmount = 50,
        ProfileCompletionThresholdBonusAmount = 2,
        VerificationBonusAmount = 3,
        ReferralBonusAmount = 2,
        IdeaSubmissionBonusAmount = 1,
        IdeaImplementedBonusAmount = 10,
    });

    private sealed class FakeSparksService : ISparksService
    {
        public int Balance { get; set; }

        public (IReadOnlyList<SparkTransaction> Items, int TotalCount) History { get; set; } = ([], 0);

        public Task SpendAsync(User user, int amount, SparkTransactionType type, Guid? referenceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах кошелька.");

        public Task RefundAsync(User user, int amount, Guid referenceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах кошелька.");

        public Task AwardAsync(User user, int amount, SparkTransactionType type, Guid? referenceId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах кошелька.");

        public Task<int> GetBalanceAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(Balance);

        public Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult(History);
    }

    private sealed class FakeUserRepository(User seed) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах кошелька.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах кошелька.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(seed.Id == id ? seed : null);

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах кошелька.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах кошелька.");
    }
}
