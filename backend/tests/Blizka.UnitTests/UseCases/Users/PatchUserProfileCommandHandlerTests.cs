using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Users;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.Users;

public sealed class PatchUserProfileCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА поле передано в запросе ТОГДА оно применяется к User")]
    public async Task Handle_applies_provided_fields_to_the_user()
    {
        var user = NewUser();
        var (handler, _) = CreateHandler(user);
        var command = new PatchUserProfileCommand(
            user.Id, "Bob", "Hi there", 180, SmokingHabit.Regularly, DrinkingHabit.No, Chronotype.EarlyBird,
            ["Favorite trip?"], DatingGoal.Friendship, "ru");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal("Bob", user.Name);
        Assert.Equal("Hi there", user.Bio);
        Assert.Equal(180, user.Height);
        Assert.Equal(SmokingHabit.Regularly, user.Smoking);
        Assert.Equal(DrinkingHabit.No, user.Drinking);
        Assert.Equal(Chronotype.EarlyBird, user.Chronotype);
        Assert.Equal(["Favorite trip?"], user.Prompts);
        Assert.Equal(DatingGoal.Friendship, user.DatingGoal);
    }

    [Fact(DisplayName = "КОГДА поле не передано (null) ТОГДА уже сохранённое значение не меняется")]
    public async Task Handle_leaves_unset_fields_untouched()
    {
        var user = NewUser();
        user.Bio = "Original bio";
        user.Height = 175;
        var (handler, _) = CreateHandler(user);
        var command = new PatchUserProfileCommand(
            user.Id, "NewName", null, null, null, null, null, null, null, "ru");

        await handler.Handle(command, CancellationToken.None);

        Assert.Equal("NewName", user.Name);
        Assert.Equal("Original bio", user.Bio);
        Assert.Equal(175, user.Height);
    }

    [Fact(DisplayName = "КОГДА редактирование впервые доводит completeness до 60% ТОГДА начисляется пороговый бонус")]
    public async Task Handle_awards_a_threshold_bonus_when_completeness_first_reaches_it()
    {
        // Базовые 35% + 3 фото (+15%) + 5 интересов (+10%) = 60% ровно.
        var user = NewUser(photoCount: 3, interestCount: 5);
        var (handler, sparkRepository) = CreateHandler(user);
        var command = new PatchUserProfileCommand(user.Id, null, null, null, null, null, null, null, null, "ru");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(60, result.Profile.ProfileCompleteness);
        Assert.Equal(2, result.SparksAwarded);
        Assert.NotNull(user.CompletenessBonus60AwardedAt);
        var transaction = Assert.Single(sparkRepository.Transactions);
        Assert.Equal(SparkTransactionType.ProfileCompletion, transaction.Type);
    }

    [Fact(DisplayName = "КОГДА порог уже был достигнут ранее ТОГДА повторное редактирование не начисляет бонус снова")]
    public async Task Handle_does_not_double_award_an_already_granted_threshold_bonus()
    {
        var user = NewUser(photoCount: 3, interestCount: 5);
        user.CompletenessBonus60AwardedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var (handler, sparkRepository) = CreateHandler(user);
        var command = new PatchUserProfileCommand(user.Id, null, "updated bio", null, null, null, null, null, null, "ru");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(60, result.Profile.ProfileCompleteness);
        Assert.Equal(0, result.SparksAwarded);
        Assert.Empty(sparkRepository.Transactions);
    }

    [Fact(DisplayName = "КОГДА имя длиннее 30 символов ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_when_name_exceeds_the_max_length()
    {
        var user = NewUser();
        var (handler, _) = CreateHandler(user);
        var command = new PatchUserProfileCommand(
            user.Id, new string('a', 31), null, null, null, null, null, null, null, "ru");

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА промптов больше трёх ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_when_there_are_too_many_prompts()
    {
        var user = NewUser();
        var (handler, _) = CreateHandler(user);
        var command = new PatchUserProfileCommand(
            user.Id, null, null, null, null, null, null, ["a", "b", "c", "d"], null, "ru");

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА параллельный PATCH того же пользователя уже сохранился первым ТОГДА выбрасывается ProfileUpdateConflictException вместо необработанного исключения")]
    public async Task Handle_translates_a_concurrent_update_conflict_into_a_profile_conflict()
    {
        var user = NewUser();
        var (handler, _) = CreateHandler(user, simulateConcurrentUpdateConflict: true);
        var command = new PatchUserProfileCommand(user.Id, "Bob", null, null, null, null, null, null, null, "ru");

        await Assert.ThrowsAsync<ProfileUpdateConflictException>(
            () => handler.Handle(command, CancellationToken.None));
    }

    private static (PatchUserProfileCommandHandler Handler, FakeSparkTransactionRepository SparkRepository) CreateHandler(
        User user, bool simulateConcurrentUpdateConflict = false)
    {
        var userRepository = new FakeUserRepository(user, simulateConcurrentUpdateConflict);
        var sparkRepository = new FakeSparkTransactionRepository();
        var sparksService = new SparksService(sparkRepository, userRepository);
        var sparksOptions = Options.Create(new SparksOptions { ProfileCompletionThresholdBonusAmount = 2 });
        var validator = new PatchUserProfileCommandValidator();

        var handler = new PatchUserProfileCommandHandler(
            userRepository, new FakeUserDatePreferenceRepository(0), sparksService, validator, sparksOptions);

        return (handler, sparkRepository);
    }

    private static User NewUser(int photoCount = 0, int interestCount = 0)
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 1, Name = "Ann", Locale = "ru" };

        for (var i = 0; i < photoCount; i++)
        {
            user.Photos.Add(new Photo
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Url = $"https://cdn.example.com/{i}.jpg",
                ThumbnailUrl = $"https://cdn.example.com/{i}-thumb.jpg",
                MediumUrl = $"https://cdn.example.com/{i}-medium.jpg",
                SortOrder = i,
                IsMain = i == 0,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        for (var i = 0; i < interestCount; i++)
        {
            user.UserInterests.Add(new UserInterest { UserId = user.Id, InterestId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow });
        }

        return user;
    }

    private sealed class FakeUserRepository(User user, bool simulateConcurrentUpdateConflict = false) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            Task.FromResult(user.TelegramId == telegramId ? user : null);

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id == id ? user : null);

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id == id ? user : null);

        public Task AddAsync(User newUser, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (simulateConcurrentUpdateConflict)
            {
                throw new ConcurrentUserUpdateException(user.Id, new InvalidOperationException("simulated xmin conflict"));
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserDatePreferenceRepository(int count) : IUserDatePreferenceRepository
    {
        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(count);
    }

    private sealed class FakeSparkTransactionRepository : ISparkTransactionRepository
    {
        public List<SparkTransaction> Transactions { get; } = [];

        public Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<SparkTransaction>, int)>(([], 0));

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
