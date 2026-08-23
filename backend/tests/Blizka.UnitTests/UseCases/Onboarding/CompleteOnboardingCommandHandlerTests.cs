using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Onboarding;

namespace Blizka.UnitTests.UseCases.Onboarding;

public sealed class CompleteOnboardingCommandHandlerTests
{
    private static readonly Guid CityId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private const string FullDraftJson =
        """{"name":"Ann","birthDate":"2000-01-01","gender":"female","showGender":"male","ageRange":{"min":20,"max":35},"datingGoals":["casual","friendship"],"cityId":"11111111-1111-1111-1111-111111111111"}""";

    [Fact(DisplayName = "КОГДА все условия выполнены и профиль минимальный ТОГДА пользователь становится Active, начисляется 50 зорок, completeness = 35%")]
    public async Task Handle_completes_onboarding_with_the_minimal_profile()
    {
        var user = NewUser(photoCount: 1);
        var draftRepository = new FakeOnboardingDraftRepository(NewDraft(user.Id, FullDraftJson));
        var sparkRepository = new FakeSparkTransactionRepository();
        var handler = CreateHandler(user, draftRepository, hasConsent: true, datePreferenceCount: 0, sparkRepository);

        var result = await handler.Handle(new CompleteOnboardingCommand(user.Id), CancellationToken.None);

        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal(50, result.SparksAwarded);
        Assert.Equal(35, result.ProfileCompleteness);
        Assert.Equal(50, user.SparksBalance);
        Assert.Equal(60, result.NextReward!.Threshold);
        Assert.Equal(2, result.NextReward.SparksReward);
        var registrationBonus = Assert.Single(sparkRepository.Transactions);
        Assert.Equal(SparkTransactionType.RegistrationBonus, registrationBonus.Type);
        Assert.Equal(50, registrationBonus.Amount);
    }

    [Fact(DisplayName = "КОГДА данные черновика переносятся в профиль ТОГДА User получает имя, дату рождения, пол, город и первую из выбранных целей")]
    public async Task Handle_copies_draft_data_onto_the_user()
    {
        var user = NewUser(photoCount: 1);
        var draftRepository = new FakeOnboardingDraftRepository(NewDraft(user.Id, FullDraftJson));
        var handler = CreateHandler(user, draftRepository, hasConsent: true, datePreferenceCount: 0, new FakeSparkTransactionRepository());

        await handler.Handle(new CompleteOnboardingCommand(user.Id), CancellationToken.None);

        Assert.Equal("Ann", user.Name);
        Assert.Equal(new DateOnly(2000, 1, 1), user.BirthDate);
        Assert.Equal(Gender.Female, user.Gender);
        Assert.Equal(CityId, user.CityId);
        Assert.Equal(DatingGoal.Casual, user.DatingGoal);
    }

    [Fact(DisplayName = "КОГДА онбординг завершён ТОГДА заводится UserFilter с ShowGender/AgeRange/DatingGoals шага 2 (T-5.4)")]
    public async Task Handle_creates_a_user_filter_from_step2_draft_data()
    {
        var user = NewUser(photoCount: 1);
        var draftRepository = new FakeOnboardingDraftRepository(NewDraft(user.Id, FullDraftJson));
        var filterRepository = new FakeUserFilterRepository();
        var handler = CreateHandler(
            user, draftRepository, hasConsent: true, datePreferenceCount: 0, new FakeSparkTransactionRepository(),
            filterRepository: filterRepository);

        await handler.Handle(new CompleteOnboardingCommand(user.Id), CancellationToken.None);

        Assert.NotNull(filterRepository.AddedFilter);
        Assert.Equal(user.Id, filterRepository.AddedFilter!.UserId);
        Assert.Equal(ShowGenderPreference.Male, filterRepository.AddedFilter.ShowGender);
        Assert.Equal(20, filterRepository.AddedFilter.AgeMin);
        Assert.Equal(35, filterRepository.AddedFilter.AgeMax);
        Assert.Equal([DatingGoal.Casual, DatingGoal.Friendship], filterRepository.AddedFilter.DatingGoals);
    }

    [Fact(DisplayName = "КОГДА профиль заполнен полностью ТОГДА completeness = 100% и начисляются все три бонуса за пороги")]
    public async Task Handle_awards_all_threshold_bonuses_for_a_fully_completed_profile()
    {
        var user = NewUser(photoCount: 3, interestCount: 5);
        user.Prompts = ["What's your favorite trip?"];
        user.IsVerified = true;
        user.VoiceIntroUrl = "https://cdn.example.com/voice.ogg";
        user.InstagramHandle = "ann";
        var draftRepository = new FakeOnboardingDraftRepository(NewDraft(user.Id, FullDraftJson));
        var sparkRepository = new FakeSparkTransactionRepository();
        var handler = CreateHandler(user, draftRepository, hasConsent: true, datePreferenceCount: 1, sparkRepository);

        var result = await handler.Handle(new CompleteOnboardingCommand(user.Id), CancellationToken.None);

        Assert.Equal(100, result.ProfileCompleteness);
        Assert.Equal(56, result.SparksAwarded);
        Assert.Null(result.NextReward);
        Assert.NotNull(user.CompletenessBonus60AwardedAt);
        Assert.NotNull(user.CompletenessBonus80AwardedAt);
        Assert.NotNull(user.CompletenessBonus100AwardedAt);
        Assert.Equal(4, sparkRepository.Transactions.Count);
    }

    [Fact(DisplayName = "КОГДА согласие не зафиксировано ТОГДА выбрасывается OnboardingIncompleteException с missingStep=consent")]
    public async Task Handle_throws_when_consent_is_missing()
    {
        var user = NewUser(photoCount: 1);
        var draftRepository = new FakeOnboardingDraftRepository(NewDraft(user.Id, FullDraftJson));
        var handler = CreateHandler(user, draftRepository, hasConsent: false, datePreferenceCount: 0, new FakeSparkTransactionRepository());

        var exception = await Assert.ThrowsAsync<OnboardingIncompleteException>(
            () => handler.Handle(new CompleteOnboardingCommand(user.Id), CancellationToken.None));

        Assert.Equal("consent", exception.MissingStep);
        Assert.Equal(UserStatus.New, user.Status);
    }

    [Fact(DisplayName = "КОГДА не загружено ни одного фото ТОГДА выбрасывается OnboardingIncompleteException с missingStep=step4")]
    public async Task Handle_throws_when_there_are_no_photos()
    {
        var user = NewUser(photoCount: 0);
        var draftRepository = new FakeOnboardingDraftRepository(NewDraft(user.Id, FullDraftJson));
        var handler = CreateHandler(user, draftRepository, hasConsent: true, datePreferenceCount: 0, new FakeSparkTransactionRepository());

        var exception = await Assert.ThrowsAsync<OnboardingIncompleteException>(
            () => handler.Handle(new CompleteOnboardingCommand(user.Id), CancellationToken.None));

        Assert.Equal("step4", exception.MissingStep);
    }

    [Fact(DisplayName = "КОГДА черновик онбординга ещё не создан ТОГДА выбрасывается OnboardingIncompleteException с missingStep=step1")]
    public async Task Handle_throws_when_there_is_no_draft_at_all()
    {
        var user = NewUser(photoCount: 1);
        var draftRepository = new FakeOnboardingDraftRepository();
        var handler = CreateHandler(user, draftRepository, hasConsent: true, datePreferenceCount: 0, new FakeSparkTransactionRepository());

        var exception = await Assert.ThrowsAsync<OnboardingIncompleteException>(
            () => handler.Handle(new CompleteOnboardingCommand(user.Id), CancellationToken.None));

        Assert.Equal("step1", exception.MissingStep);
    }

    [Fact(DisplayName = "КОГДА онбординг уже был завершён ранее ТОГДА выбрасывается OnboardingAlreadyCompletedException")]
    public async Task Handle_throws_when_the_user_is_already_active()
    {
        var user = NewUser(photoCount: 1);
        user.Status = UserStatus.Active;
        var draftRepository = new FakeOnboardingDraftRepository(NewDraft(user.Id, FullDraftJson));
        var sparkRepository = new FakeSparkTransactionRepository();
        var handler = CreateHandler(user, draftRepository, hasConsent: true, datePreferenceCount: 0, sparkRepository);

        await Assert.ThrowsAsync<OnboardingAlreadyCompletedException>(
            () => handler.Handle(new CompleteOnboardingCommand(user.Id), CancellationToken.None));

        Assert.Empty(sparkRepository.Transactions);
    }

    [Fact(DisplayName = "КОГДА два параллельных запроса завершают онбординг одного пользователя ТОГДА проигравший получает OnboardingAlreadyCompletedException вместо задвоенного начисления")]
    public async Task Handle_translates_a_concurrent_update_conflict_into_already_completed()
    {
        var user = NewUser(photoCount: 1);
        var draftRepository = new FakeOnboardingDraftRepository(NewDraft(user.Id, FullDraftJson));
        var sparkRepository = new FakeSparkTransactionRepository();
        var handler = CreateHandler(
            user, draftRepository, hasConsent: true, datePreferenceCount: 0, sparkRepository, simulateConcurrentUpdateConflict: true);

        await Assert.ThrowsAsync<OnboardingAlreadyCompletedException>(
            () => handler.Handle(new CompleteOnboardingCommand(user.Id), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА порог 60% достигнут, но бонус за него уже был начислен ранее ТОГДА повторно он не начисляется")]
    public async Task Handle_does_not_double_award_an_already_granted_threshold_bonus()
    {
        // 3+ фото (+15) и 5+ интересов (+10) поверх базовых 35% дают ровно 60% — порог достигнут этим же вызовом,
        // но CompletenessBonus60AwardedAt уже проставлен заранее (симулирует более раннее начисление).
        var user = NewUser(photoCount: 3, interestCount: 5);
        user.CompletenessBonus60AwardedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var draftRepository = new FakeOnboardingDraftRepository(NewDraft(user.Id, FullDraftJson));
        var sparkRepository = new FakeSparkTransactionRepository();
        var handler = CreateHandler(user, draftRepository, hasConsent: true, datePreferenceCount: 0, sparkRepository);

        var result = await handler.Handle(new CompleteOnboardingCommand(user.Id), CancellationToken.None);

        Assert.Equal(60, result.ProfileCompleteness);
        Assert.Equal(50, result.SparksAwarded);
        Assert.Equal(80, result.NextReward!.Threshold);
        Assert.DoesNotContain(sparkRepository.Transactions, t => t.Type == SparkTransactionType.ProfileCompletion);
    }

    private static CompleteOnboardingCommandHandler CreateHandler(
        User user,
        FakeOnboardingDraftRepository draftRepository,
        bool hasConsent,
        int datePreferenceCount,
        FakeSparkTransactionRepository sparkRepository,
        bool simulateConcurrentUpdateConflict = false,
        FakeUserFilterRepository? filterRepository = null) =>
        new(
            new FakeUserRepository(user, simulateConcurrentUpdateConflict),
            draftRepository,
            new FakeUserConsentRepository(hasConsent),
            new FakeUserDatePreferenceRepository(datePreferenceCount),
            sparkRepository,
            filterRepository ?? new FakeUserFilterRepository());

    private static User NewUser(int photoCount, int interestCount = 0)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            TelegramId = 1,
            Status = UserStatus.New,
            Name = string.Empty,
            Locale = "ru",
        };

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

    private static OnboardingDraft NewDraft(Guid userId, string dataJson) =>
        new() { UserId = userId, Step = 3, DataJson = dataJson, UpdatedAt = DateTimeOffset.UtcNow };

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

    private sealed class FakeOnboardingDraftRepository(params OnboardingDraft[] seed) : IOnboardingDraftRepository
    {
        private readonly List<OnboardingDraft> _drafts = [.. seed];

        public Task<OnboardingDraft?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(_drafts.SingleOrDefault(d => d.UserId == userId));

        public Task AddAsync(OnboardingDraft draft, CancellationToken cancellationToken)
        {
            _drafts.Add(draft);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUserConsentRepository(bool hasConsent) : IUserConsentRepository
    {
        public Task AddAsync(UserConsent consent, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<bool> HasConsentAsync(Guid userId, ConsentType type, CancellationToken cancellationToken) =>
            Task.FromResult(hasConsent);

        public Task<List<UserConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Список согласий не используется в тестах завершения онбординга.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUserFilterRepository : IUserFilterRepository
    {
        public UserFilter? AddedFilter { get; private set; }

        public Task<UserFilter?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(AddedFilter?.UserId == userId ? AddedFilter : null);

        public Task AddAsync(UserFilter filter, CancellationToken cancellationToken)
        {
            AddedFilter = filter;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
