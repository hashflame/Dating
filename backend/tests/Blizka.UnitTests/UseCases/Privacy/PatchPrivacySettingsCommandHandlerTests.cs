using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Subscriptions;
using Blizka.App.UseCases.Privacy;

namespace Blizka.UnitTests.UseCases.Privacy;

public sealed class PatchPrivacySettingsCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА у пользователя ещё нет строки ТОГДА PATCH создаёт её с переданными полями и дефолтом showLastActive=true")]
    public async Task Handle_creates_a_new_row_when_none_exists()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePrivacySettingsRepository();
        var handler = new PatchPrivacySettingsCommandHandler(repository);
        var command = new PatchPrivacySettingsCommand(userId, BlockIncomingMessages: true, HideDistance: null, HideAge: null, ShowLastActive: null, InvisibleMode: null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.BlockIncomingMessages);
        Assert.True(result.ShowLastActive);
        var stored = Assert.Single(repository.Added);
        Assert.Equal(userId, stored.UserId);
    }

    [Fact(DisplayName = "КОГДА поле не передано (null) ТОГДА текущее значение не меняется")]
    public async Task Handle_leaves_unset_fields_untouched()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePrivacySettingsRepository();
        repository.ByUserId[userId] = new PrivacySettings { UserId = userId, HideAge = true, ShowLastActive = true };
        var handler = new PatchPrivacySettingsCommandHandler(repository);
        var command = new PatchPrivacySettingsCommand(userId, BlockIncomingMessages: true, HideDistance: null, HideAge: null, ShowLastActive: null, InvisibleMode: null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.BlockIncomingMessages);
        Assert.True(result.HideAge);
        Assert.False(result.HideDistance);
        Assert.Empty(repository.Added);
    }

    [Fact(DisplayName = "КОГДА два параллельных первых PATCH создают строку одновременно ТОГДА конфликт подхватывается, а не падает в 500")]
    public async Task Handle_recovers_from_a_concurrent_first_patch_instead_of_failing()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePrivacySettingsRepository
        {
            ConcurrentWinner = new PrivacySettings { UserId = userId, HideAge = true },
        };
        var handler = new PatchPrivacySettingsCommandHandler(repository);
        var command = new PatchPrivacySettingsCommand(userId, BlockIncomingMessages: true, null, null, null, null);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.BlockIncomingMessages);
        Assert.True(result.HideAge);
        var stored = Assert.Single(repository.ByUserId.Values);
        Assert.True(stored.BlockIncomingMessages);
        Assert.True(stored.HideAge);
    }

    [Fact(DisplayName = "КОГДА нет ISubscriptionChecker в DI и invisibleMode=true ТОГДА выбрасывается InvisibleModeRequiresSubscriptionException")]
    public async Task Handle_rejects_invisible_mode_without_a_subscription_checker()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePrivacySettingsRepository();
        var handler = new PatchPrivacySettingsCommandHandler(repository);
        var command = new PatchPrivacySettingsCommand(userId, null, null, null, null, InvisibleMode: true);

        await Assert.ThrowsAsync<InvisibleModeRequiresSubscriptionException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА подписка неактивна ТОГДА включение invisibleMode отклоняется")]
    public async Task Handle_rejects_invisible_mode_when_the_subscription_is_inactive()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePrivacySettingsRepository();
        var handler = new PatchPrivacySettingsCommandHandler(repository, new FakeSubscriptionChecker(false));
        var command = new PatchPrivacySettingsCommand(userId, null, null, null, null, InvisibleMode: true);

        await Assert.ThrowsAsync<InvisibleModeRequiresSubscriptionException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА подписка активна ТОГДА включение invisibleMode проходит")]
    public async Task Handle_allows_invisible_mode_with_an_active_subscription()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePrivacySettingsRepository();
        var handler = new PatchPrivacySettingsCommandHandler(repository, new FakeSubscriptionChecker(true));
        var command = new PatchPrivacySettingsCommand(userId, null, null, null, null, InvisibleMode: true);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.InvisibleMode);
    }

    [Fact(DisplayName = "КОГДА invisibleMode уже включён и PATCH передаёт true повторно ТОГДА подписка не проверяется заново")]
    public async Task Handle_does_not_recheck_subscription_when_invisible_mode_is_already_on()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePrivacySettingsRepository();
        repository.ByUserId[userId] = new PrivacySettings { UserId = userId, InvisibleMode = true };
        var handler = new PatchPrivacySettingsCommandHandler(repository);
        var command = new PatchPrivacySettingsCommand(userId, null, null, null, null, InvisibleMode: true);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.InvisibleMode);
    }

    private sealed class FakeSubscriptionChecker(bool hasActiveSubscription) : ISubscriptionChecker
    {
        public Task<bool> HasUnlimitedSwipesAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах приватности.");

        public Task<bool> HasUnlimitedContactUnlocksAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах приватности.");

        public Task<bool> HasActiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(hasActiveSubscription);
    }

    private sealed class FakePrivacySettingsRepository : IPrivacySettingsRepository
    {
        private readonly List<PrivacySettings> _pending = [];

        public Dictionary<Guid, PrivacySettings> ByUserId { get; } = [];

        public List<PrivacySettings> Added { get; } = [];

        /// <summary>Когда задано, следующий SaveChangesAsync симулирует конкурентную вставку строки с тем же UserId — "чужая" строка фиксируется первой, а наша попытка добавить новую падает с ConcurrentPrivacySettingsCreationException.</summary>
        public PrivacySettings? ConcurrentWinner { get; set; }

        public Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах PATCH.");

        public Task<PrivacySettings?> GetByUserIdTrackedAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(ByUserId.GetValueOrDefault(userId));

        public Task<IReadOnlyDictionary<Guid, PrivacySettings>> GetByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах PATCH.");

        public Task AddAsync(PrivacySettings settings, CancellationToken cancellationToken)
        {
            Added.Add(settings);
            _pending.Add(settings);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (ConcurrentWinner is { } winner)
            {
                ConcurrentWinner = null;
                ByUserId[winner.UserId] = winner;
                _pending.Clear();
                throw new ConcurrentPrivacySettingsCreationException(winner.UserId, new InvalidOperationException("simulated unique violation"));
            }

            foreach (var settings in _pending)
            {
                ByUserId[settings.UserId] = settings;
            }

            _pending.Clear();
            return Task.CompletedTask;
        }
    }
}
