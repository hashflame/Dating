using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Users;

namespace Blizka.UnitTests.UseCases.Users;

public sealed class BuildDataExportQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА у пользователя есть фото, интересы, согласия и настройки приватности ТОГДА все они попадают в архив")]
    public async Task Handle_builds_a_payload_with_all_available_data()
    {
        var interest = new Interest { Id = Guid.NewGuid(), NameRu = "Кино" };
        var city = new City { Id = Guid.NewGuid(), NameRu = "Минск" };
        var photo = new Photo { Id = Guid.NewGuid(), Url = "https://example.com/photo.jpg", SortOrder = 0, IsMain = true, CreatedAt = DateTimeOffset.UtcNow };
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 42,
            TelegramUsername = "ann",
            Name = "Аня",
            BirthDate = new DateOnly(2000, 1, 1),
            Gender = Gender.Female,
            City = city,
            Bio = "Привет",
            Locale = "ru",
            Status = UserStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            Photos = [photo],
            UserInterests = [new UserInterest { Interest = interest }],
        };
        var consent = new UserConsent { Type = ConsentType.TermsAndPrivacyPolicy, Version = "1.0", Timestamp = DateTimeOffset.UtcNow, AgeConfirmed = true };
        var privacySettings = new PrivacySettings { UserId = user.Id, HideAge = true, ShowLastActive = true };
        var handler = new BuildDataExportQueryHandler(
            new FakeUserRepository(user), new FakeUserConsentRepository([consent]), new FakePrivacySettingsRepository(privacySettings));

        var result = await handler.Handle(new BuildDataExportQuery(user.Id), CancellationToken.None);

        Assert.Equal(user.Id, result.Profile.UserId);
        Assert.Equal("Аня", result.Profile.Name);
        Assert.Equal("Минск", result.Profile.CityName);
        Assert.Equal(nameof(Gender.Female), result.Profile.Gender);
        var resultPhoto = Assert.Single(result.Photos);
        Assert.Equal(photo.Url, resultPhoto.Url);
        var resultInterest = Assert.Single(result.Interests);
        Assert.Equal("Кино", resultInterest);
        var resultConsent = Assert.Single(result.Consents);
        Assert.Equal(nameof(ConsentType.TermsAndPrivacyPolicy), resultConsent.Type);
        Assert.NotNull(result.PrivacySettings);
        Assert.True(result.PrivacySettings!.HideAge);
    }

    [Fact(DisplayName = "КОГДА у пользователя ещё нет настроек приватности ТОГДА в архиве PrivacySettings равен null")]
    public async Task Handle_returns_null_privacy_settings_when_none_are_stored()
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 1, Name = "Аня", Locale = "ru", CreatedAt = DateTimeOffset.UtcNow };
        var handler = new BuildDataExportQueryHandler(
            new FakeUserRepository(user), new FakeUserConsentRepository([]), new FakePrivacySettingsRepository(privacySettings: null));

        var result = await handler.Handle(new BuildDataExportQuery(user.Id), CancellationToken.None);

        Assert.Null(result.PrivacySettings);
        Assert.Empty(result.Photos);
        Assert.Empty(result.Interests);
        Assert.Empty(result.Consents);
    }

    [Fact(DisplayName = "КОГДА аутентифицированный пользователь не найден в репозитории ТОГДА выбрасывается InvalidOperationException")]
    public async Task Handle_throws_when_the_authenticated_user_is_missing()
    {
        var handler = new BuildDataExportQueryHandler(
            new FakeUserRepository(seed: null), new FakeUserConsentRepository([]), new FakePrivacySettingsRepository(privacySettings: null));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new BuildDataExportQuery(Guid.NewGuid()), CancellationToken.None));
    }

    private sealed class FakeUserRepository(User? seed) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BuildDataExportQueryHandler.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(seed?.Id == id ? seed : null);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BuildDataExportQueryHandler.");

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BuildDataExportQueryHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BuildDataExportQueryHandler.");
    }

    private sealed class FakeUserConsentRepository(List<UserConsent> consents) : IUserConsentRepository
    {
        public Task AddAsync(UserConsent consent, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BuildDataExportQueryHandler.");

        public Task<bool> HasConsentAsync(Guid userId, ConsentType type, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BuildDataExportQueryHandler.");

        public Task<List<UserConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(consents);

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BuildDataExportQueryHandler.");
    }

    private sealed class FakePrivacySettingsRepository(PrivacySettings? privacySettings) : IPrivacySettingsRepository
    {
        public Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(privacySettings);

        public Task<PrivacySettings?> GetByUserIdTrackedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BuildDataExportQueryHandler.");

        public Task<IReadOnlyDictionary<Guid, PrivacySettings>> GetByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BuildDataExportQueryHandler.");

        public Task AddAsync(PrivacySettings settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BuildDataExportQueryHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах BuildDataExportQueryHandler.");
    }
}
