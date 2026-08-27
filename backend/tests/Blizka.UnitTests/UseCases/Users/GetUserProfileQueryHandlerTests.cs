using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Users;

namespace Blizka.UnitTests.UseCases.Users;

public sealed class GetUserProfileQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА пользователь найден ТОГДА возвращаются имя, возраст, город, фото, интересы и промпты")]
    public async Task Handle_returns_the_target_users_profile()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 42,
            Name = "Ann",
            Gender = Gender.Female,
            BirthDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date.AddYears(-25)),
            Bio = "Hello",
            Prompts = ["Favorite trip?"],
            DatingGoal = DatingGoal.Casual,
            IsVerified = true,
            Status = UserStatus.Active,
        };
        var handler = new GetUserProfileQueryHandler(new FakeUserRepository(user), new FakeUserBlockRepository(), new FakePrivacySettingsRepository());

        var result = await handler.Handle(new GetUserProfileQuery(user.Id, Guid.NewGuid(), "ru"), CancellationToken.None);

        Assert.Equal(user.Id, result.UserId);
        Assert.Equal("Ann", result.Name);
        Assert.Equal(25, result.Age);
        Assert.Equal("Hello", result.Bio);
        Assert.Equal(["Favorite trip?"], result.Prompts);
        Assert.Equal(DatingGoal.Casual, result.DatingGoal);
        Assert.True(result.IsVerified);
    }

    [Fact(DisplayName = "КОГДА пользователь не найден ТОГДА выбрасывается UserProfileNotFoundException")]
    public async Task Handle_throws_when_the_target_user_is_missing()
    {
        var handler = new GetUserProfileQueryHandler(
            new FakeUserRepository(user: null), new FakeUserBlockRepository(), new FakePrivacySettingsRepository());

        await Assert.ThrowsAsync<UserProfileNotFoundException>(
            () => handler.Handle(new GetUserProfileQuery(Guid.NewGuid(), Guid.NewGuid(), "ru"), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА аккаунт удалён (Status = Deleted) ТОГДА анкета недоступна — UserProfileNotFoundException, а не 200")]
    public async Task Handle_throws_for_a_deleted_account()
    {
        var deletedUser = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 1,
            Name = "Gone",
            Status = UserStatus.Deleted,
            BirthDate = new DateOnly(1995, 1, 1),
        };
        var handler = new GetUserProfileQueryHandler(
            new FakeUserRepository(deletedUser), new FakeUserBlockRepository(), new FakePrivacySettingsRepository());

        await Assert.ThrowsAsync<UserProfileNotFoundException>(
            () => handler.Handle(new GetUserProfileQuery(deletedUser.Id, Guid.NewGuid(), "ru"), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА просматриваемый пользователь включил hideAge ТОГДА возраст в анкете null")]
    public async Task Handle_hides_the_age_when_the_target_user_enabled_hide_age()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 42,
            Name = "Ann",
            Gender = Gender.Female,
            BirthDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date.AddYears(-25)),
            Status = UserStatus.Active,
        };
        var privacyRepository = new FakePrivacySettingsRepository();
        privacyRepository.ByUserId[user.Id] = new PrivacySettings { UserId = user.Id, HideAge = true };
        var handler = new GetUserProfileQueryHandler(new FakeUserRepository(user), new FakeUserBlockRepository(), privacyRepository);

        var result = await handler.Handle(new GetUserProfileQuery(user.Id, Guid.NewGuid(), "ru"), CancellationToken.None);

        Assert.Null(result.Age);
    }

    [Fact(DisplayName = "КОГДА между пользователями есть блокировка (в любом направлении) ТОГДА анкета недоступна — UserProfileNotFoundException, а не 200")]
    public async Task Handle_throws_when_users_have_blocked_each_other()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 42,
            Name = "Ann",
            BirthDate = new DateOnly(1995, 1, 1),
            Status = UserStatus.Active,
        };
        var requestingUserId = Guid.NewGuid();
        var handler = new GetUserProfileQueryHandler(
            new FakeUserRepository(user), new FakeUserBlockRepository(blocked: true), new FakePrivacySettingsRepository());

        await Assert.ThrowsAsync<UserProfileNotFoundException>(
            () => handler.Handle(new GetUserProfileQuery(user.Id, requestingUserId, "ru"), CancellationToken.None));
    }

    private sealed class FakeUserRepository(User? user) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(user?.Id == id ? user : null);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");
    }

    private sealed class FakeUserBlockRepository(bool blocked = false) : IUserBlockRepository
    {
        public Task<bool> ExistsAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");

        public Task<bool> ExistsEitherDirectionAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken) =>
            Task.FromResult(blocked);

        public Task<IReadOnlyList<UserBlock>> GetBlockedByUserAsync(Guid blockerUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");

        public Task AddAsync(UserBlock block, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");

        public Task RemoveAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");
    }

    private sealed class FakePrivacySettingsRepository : IPrivacySettingsRepository
    {
        public Dictionary<Guid, PrivacySettings> ByUserId { get; } = [];

        public Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(ByUserId.GetValueOrDefault(userId));

        public Task<PrivacySettings?> GetByUserIdTrackedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");

        public Task<IReadOnlyDictionary<Guid, PrivacySettings>> GetByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");

        public Task AddAsync(PrivacySettings settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetUserProfileQueryHandler.");
    }
}
