using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Privacy;

namespace Blizka.UnitTests.UseCases.Privacy;

public sealed class GetPrivacySettingsQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА у пользователя ещё нет строки в PrivacySettings ТОГДА возвращаются дефолты (всё выключено, кроме showLastActive)")]
    public async Task Handle_returns_defaults_when_no_row_exists()
    {
        var repository = new FakePrivacySettingsRepository();
        var handler = new GetPrivacySettingsQueryHandler(repository);

        var result = await handler.Handle(new GetPrivacySettingsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.BlockIncomingMessages);
        Assert.False(result.HideDistance);
        Assert.False(result.HideAge);
        Assert.True(result.ShowLastActive);
        Assert.False(result.InvisibleMode);
    }

    [Fact(DisplayName = "КОГДА у пользователя есть сохранённые настройки ТОГДА возвращаются они, а не дефолты")]
    public async Task Handle_returns_the_stored_row_when_it_exists()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePrivacySettingsRepository();
        repository.ByUserId[userId] = new PrivacySettings
        {
            UserId = userId,
            BlockIncomingMessages = true,
            HideDistance = true,
            HideAge = true,
            ShowLastActive = false,
            InvisibleMode = false,
        };
        var handler = new GetPrivacySettingsQueryHandler(repository);

        var result = await handler.Handle(new GetPrivacySettingsQuery(userId), CancellationToken.None);

        Assert.True(result.BlockIncomingMessages);
        Assert.True(result.HideDistance);
        Assert.True(result.HideAge);
        Assert.False(result.ShowLastActive);
    }

    private sealed class FakePrivacySettingsRepository : IPrivacySettingsRepository
    {
        public Dictionary<Guid, PrivacySettings> ByUserId { get; } = [];

        public Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(ByUserId.GetValueOrDefault(userId));

        public Task<PrivacySettings?> GetByUserIdTrackedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GET.");

        public Task<IReadOnlyDictionary<Guid, PrivacySettings>> GetByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GET.");

        public Task AddAsync(PrivacySettings settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GET.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GET.");
    }
}
