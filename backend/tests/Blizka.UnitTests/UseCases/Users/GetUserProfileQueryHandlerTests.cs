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
        var handler = new GetUserProfileQueryHandler(new FakeUserRepository(user));

        var result = await handler.Handle(new GetUserProfileQuery(user.Id, "ru"), CancellationToken.None);

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
        var handler = new GetUserProfileQueryHandler(new FakeUserRepository(user: null));

        await Assert.ThrowsAsync<UserProfileNotFoundException>(
            () => handler.Handle(new GetUserProfileQuery(Guid.NewGuid(), "ru"), CancellationToken.None));
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
        var handler = new GetUserProfileQueryHandler(new FakeUserRepository(deletedUser));

        await Assert.ThrowsAsync<UserProfileNotFoundException>(
            () => handler.Handle(new GetUserProfileQuery(deletedUser.Id, "ru"), CancellationToken.None));
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
}
