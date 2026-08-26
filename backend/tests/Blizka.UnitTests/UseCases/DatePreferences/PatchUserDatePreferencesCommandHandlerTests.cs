using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.DatePreferences;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.DatePreferences;

public sealed class PatchUserDatePreferencesCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА переданы существующие в каталоге коды ТОГДА они становятся предпочтениями пользователя")]
    public async Task Handle_replaces_user_date_preferences_with_the_selected_catalog_codes()
    {
        var user = NewUser();
        var activeOutdoors = NewCatalogPreference(DatePreferenceCode.ActiveOutdoors);
        var calmHangout = NewCatalogPreference(DatePreferenceCode.CalmHangout);
        var (handler, _) = CreateHandler(user, [activeOutdoors, calmHangout]);
        var command = new PatchUserDatePreferencesCommand(user.Id, [DatePreferenceCode.ActiveOutdoors, DatePreferenceCode.CalmHangout], "ru");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, user.UserDatePreferences.Count);
        Assert.Contains(user.UserDatePreferences, p => p.DatePreferenceId == activeOutdoors.Id);
        Assert.Contains(user.UserDatePreferences, p => p.DatePreferenceId == calmHangout.Id);
        Assert.Equal(2, result.Preferences.Count);
    }

    [Fact(DisplayName = "КОГДА patch не включает ранее выбранное предпочтение ТОГДА оно удаляется у пользователя (замена, а не добавление)")]
    public async Task Handle_removes_previously_selected_preferences_not_in_the_new_set()
    {
        var user = NewUser();
        var activeOutdoors = NewCatalogPreference(DatePreferenceCode.ActiveOutdoors);
        var calmHangout = NewCatalogPreference(DatePreferenceCode.CalmHangout);
        user.UserDatePreferences.Add(new UserDatePreference
        {
            UserId = user.Id, DatePreferenceId = activeOutdoors.Id, CreatedAt = DateTimeOffset.UtcNow,
        });
        var (handler, _) = CreateHandler(user, [activeOutdoors, calmHangout]);
        var command = new PatchUserDatePreferencesCommand(user.Id, [DatePreferenceCode.CalmHangout], "ru");

        await handler.Handle(command, CancellationToken.None);

        var onlyPreference = Assert.Single(user.UserDatePreferences);
        Assert.Equal(calmHangout.Id, onlyPreference.DatePreferenceId);
    }

    [Fact(DisplayName = "КОГДА выбор первого предпочтения впервые доводит completeness до 60% ТОГДА начисляется пороговый бонус")]
    public async Task Handle_awards_a_threshold_bonus_when_completeness_first_reaches_it()
    {
        // Базовые 35% + 3 фото (+15%) + предпочтение по свиданию (+10%) = 60% ровно.
        var user = NewUser(photoCount: 3);
        var calmHangout = NewCatalogPreference(DatePreferenceCode.CalmHangout);
        var (handler, sparkRepository) = CreateHandler(user, [calmHangout]);
        var command = new PatchUserDatePreferencesCommand(user.Id, [DatePreferenceCode.CalmHangout], "ru");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(60, result.Profile.ProfileCompleteness);
        Assert.Equal(2, result.SparksAwarded);
        var transaction = Assert.Single(sparkRepository.Transactions);
        Assert.Equal(SparkTransactionType.ProfileCompletion, transaction.Type);
    }

    [Fact(DisplayName = "КОГДА параллельный PATCH того же пользователя уже сохранился первым ТОГДА выбрасывается ProfileUpdateConflictException вместо необработанного исключения")]
    public async Task Handle_translates_a_concurrent_update_conflict_into_a_profile_conflict()
    {
        var user = NewUser();
        var calmHangout = NewCatalogPreference(DatePreferenceCode.CalmHangout);
        var (handler, _) = CreateHandler(user, [calmHangout], simulateConcurrentUpdateConflict: true);
        var command = new PatchUserDatePreferencesCommand(user.Id, [DatePreferenceCode.CalmHangout], "ru");

        await Assert.ThrowsAsync<ProfileUpdateConflictException>(() => handler.Handle(command, CancellationToken.None));
    }

    private static (PatchUserDatePreferencesCommandHandler Handler, FakeSparkTransactionRepository SparkRepository) CreateHandler(
        User user, IReadOnlyCollection<DatePreference> catalog, bool simulateConcurrentUpdateConflict = false)
    {
        var userRepository = new FakeUserRepository(user, simulateConcurrentUpdateConflict);
        var datePreferenceRepository = new FakeUserDatePreferenceRepository(catalog);
        var sparkRepository = new FakeSparkTransactionRepository();
        var sparksService = new SparksService(sparkRepository, userRepository);
        var sparksOptions = Options.Create(new SparksOptions { ProfileCompletionThresholdBonusAmount = 2 });
        var validator = new PatchUserDatePreferencesCommandValidator();

        var handler = new PatchUserDatePreferencesCommandHandler(
            userRepository, datePreferenceRepository, sparksService, validator, sparksOptions);

        return (handler, sparkRepository);
    }

    private static User NewUser(int photoCount = 0)
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

        return user;
    }

    private static DatePreference NewCatalogPreference(DatePreferenceCode code) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        NameRu = code.ToString(),
        NameBe = code.ToString(),
        NameEn = code.ToString(),
    };

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

    private sealed class FakeUserDatePreferenceRepository(IEnumerable<DatePreference> catalog) : IUserDatePreferenceRepository
    {
        private readonly List<DatePreference> _catalog = [.. catalog];

        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется — PatchUserDatePreferencesCommandHandler считает по загруженной коллекции User.");

        public Task<IReadOnlyList<DatePreference>> GetCatalogAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DatePreference>>(_catalog);
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
