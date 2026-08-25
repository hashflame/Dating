using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Users;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.Users;

public sealed class GetMeQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА пользователь найден ТОГДА возвращаются базовые и редактируемые поля профиля, баланс зорок, статус, локаль и completeness")]
    public async Task Handle_returns_the_users_full_profile()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 42,
            Name = "Ann",
            Gender = Gender.Female,
            BirthDate = new DateOnly(2000, 1, 1),
            Bio = "Hello",
            Height = 170,
            Smoking = SmokingHabit.No,
            Drinking = DrinkingHabit.Sometimes,
            Chronotype = Chronotype.NightOwl,
            Prompts = ["Favorite trip?"],
            DatingGoal = DatingGoal.Casual,
            SparksBalance = 7,
            Status = UserStatus.Active,
            Locale = "be",
        };
        var handler = CreateHandler(user, datePreferenceCount: 0);

        var result = await handler.Handle(new GetMeQuery(user.Id, "ru"), CancellationToken.None);

        Assert.Equal(user.Id, result.Id);
        Assert.Equal(42, result.TelegramId);
        Assert.Equal("Ann", result.Name);
        Assert.Equal(Gender.Female, result.Gender);
        Assert.Equal(new DateOnly(2000, 1, 1), result.BirthDate);
        Assert.Equal("Hello", result.Bio);
        Assert.Equal(170, result.Height);
        Assert.Equal(SmokingHabit.No, result.Smoking);
        Assert.Equal(DrinkingHabit.Sometimes, result.Drinking);
        Assert.Equal(Chronotype.NightOwl, result.Chronotype);
        Assert.Equal(["Favorite trip?"], result.Prompts);
        Assert.Equal(DatingGoal.Casual, result.DatingGoal);
        Assert.Equal(7, result.SparksBalance);
        Assert.Equal(UserStatus.Active, result.Status);
        Assert.Equal("be", result.Locale);
        // 35% базовых + 10% за непустые Prompts.
        Assert.Equal(45, result.ProfileCompleteness);
        Assert.Equal(60, result.NextReward!.Threshold);
    }

    [Fact(DisplayName = "КОГДА NextReward локализуется ТОГДА используется локаль запроса (аргумент Locale), а не персистентная User.Locale")]
    public async Task Handle_localizes_the_next_reward_hint_using_the_request_locale()
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 1, Name = "Ann", Locale = "ru" };
        var handler = CreateHandler(user, datePreferenceCount: 0);

        var result = await handler.Handle(new GetMeQuery(user.Id, "en"), CancellationToken.None);

        Assert.Contains("bonus", result.NextReward!.Hint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "КОГДА аутентифицированный пользователь не найден в репозитории ТОГДА выбрасывается InvalidOperationException")]
    public async Task Handle_throws_when_the_authenticated_user_is_missing()
    {
        var handler = CreateHandler(user: null, datePreferenceCount: 0);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.Handle(new GetMeQuery(Guid.NewGuid(), "ru"), CancellationToken.None));
    }

    private static GetMeQueryHandler CreateHandler(User? user, int datePreferenceCount)
    {
        var sparksOptions = Options.Create(new SparksOptions { ProfileCompletionThresholdBonusAmount = 2 });
        return new(
            new FakeUserRepository(user),
            new FakeUserDatePreferenceRepository(datePreferenceCount),
            sparksOptions);
    }

    private sealed class FakeUserRepository(User? seed) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetMeQueryHandler.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(seed?.Id == id ? seed : null);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetMeQueryHandler.");

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetMeQueryHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах GetMeQueryHandler.");
    }

    private sealed class FakeUserDatePreferenceRepository(int count) : IUserDatePreferenceRepository
    {
        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(count);
    }
}
