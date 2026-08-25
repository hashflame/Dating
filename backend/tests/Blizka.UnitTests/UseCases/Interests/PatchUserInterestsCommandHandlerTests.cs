using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Interests;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.Interests;

public sealed class PatchUserInterestsCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА переданы существующие interestIds ТОГДА они становятся интересами пользователя")]
    public async Task Handle_replaces_user_interests_with_the_selected_catalog_ids()
    {
        var user = NewUser();
        var running = NewCatalogInterest("Бег");
        var yoga = NewCatalogInterest("Йога");
        var (handler, interestRepository, _) = CreateHandler(user, running, yoga);
        var command = new PatchUserInterestsCommand(user.Id, [running.Id, yoga.Id], [], "ru");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal(2, user.UserInterests.Count);
        Assert.Contains(user.UserInterests, ui => ui.InterestId == running.Id);
        Assert.Contains(user.UserInterests, ui => ui.InterestId == yoga.Id);
        Assert.Equal(2, result.Interests.Count);
        Assert.Empty(interestRepository.Added);
    }

    [Fact(DisplayName = "КОГДА patch не включает ранее выбранный интерес ТОГДА он удаляется у пользователя (замена, а не добавление)")]
    public async Task Handle_removes_previously_selected_interests_not_in_the_new_set()
    {
        var user = NewUser();
        var running = NewCatalogInterest("Бег");
        var yoga = NewCatalogInterest("Йога");
        user.UserInterests.Add(new UserInterest { UserId = user.Id, InterestId = running.Id, CreatedAt = DateTimeOffset.UtcNow });
        var (handler, _, _) = CreateHandler(user, running, yoga);
        var command = new PatchUserInterestsCommand(user.Id, [yoga.Id], [], "ru");

        await handler.Handle(command, CancellationToken.None);

        var onlyInterest = Assert.Single(user.UserInterests);
        Assert.Equal(yoga.Id, onlyInterest.InterestId);
    }

    [Fact(DisplayName = "КОГДА кастомное название не найдено в каталоге ТОГДА создаётся новый общий Interest с IsCustom=true")]
    public async Task Handle_creates_a_new_shared_custom_interest_when_not_found_in_the_catalog()
    {
        var user = NewUser();
        var (handler, interestRepository, _) = CreateHandler(user);
        var command = new PatchUserInterestsCommand(user.Id, [], ["Скалолазание"], "ru");

        var result = await handler.Handle(command, CancellationToken.None);

        var created = Assert.Single(interestRepository.Added);
        Assert.True(created.IsCustom);
        Assert.Equal("Скалолазание", created.NameRu);
        Assert.Contains(user.UserInterests, ui => ui.InterestId == created.Id);
        Assert.Contains(result.Interests, i => i.Id == created.Id && i.IsCustom);
    }

    [Fact(DisplayName = "КОГДА кастомное название уже существует (регистронезависимо) ТОГДА переиспользуется существующий интерес, а не создаётся дубликат")]
    public async Task Handle_reuses_an_existing_interest_by_name_instead_of_duplicating()
    {
        var user = NewUser();
        var existingCustom = NewCatalogInterest("Скалолазание", isCustom: true);
        var (handler, interestRepository, _) = CreateHandler(user, existingCustom);
        var command = new PatchUserInterestsCommand(user.Id, [], ["скалолазание"], "ru");

        await handler.Handle(command, CancellationToken.None);

        Assert.Empty(interestRepository.Added);
        var onlyInterest = Assert.Single(user.UserInterests);
        Assert.Equal(existingCustom.Id, onlyInterest.InterestId);
    }

    [Fact(DisplayName = "КОГДА один из interestIds отсутствует в каталоге ТОГДА выбрасывается InterestNotFoundException")]
    public async Task Handle_throws_when_an_interest_id_is_not_in_the_catalog()
    {
        var user = NewUser();
        var (handler, _, _) = CreateHandler(user);
        var command = new PatchUserInterestsCommand(user.Id, [Guid.NewGuid()], [], "ru");

        await Assert.ThrowsAsync<InterestNotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА суммарно больше 20 интересов ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_when_the_total_count_exceeds_the_limit()
    {
        var user = NewUser();
        var (handler, _, _) = CreateHandler(user);
        var customNames = Enumerable.Range(0, 21).Select(i => $"Интерес {i}").ToArray();
        var command = new PatchUserInterestsCommand(user.Id, [], customNames, "ru");

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА выбор пятого интереса впервые доводит completeness до 60% ТОГДА начисляется пороговый бонус")]
    public async Task Handle_awards_a_threshold_bonus_when_completeness_first_reaches_it()
    {
        // Базовые 35% + 3 фото (+15%) + 5 интересов (+10%) = 60% ровно.
        var user = NewUser(photoCount: 3);
        var interests = Enumerable.Range(0, 5).Select(i => NewCatalogInterest($"Интерес {i}")).ToArray();
        var (handler, _, sparkRepository) = CreateHandler(user, interests);
        var command = new PatchUserInterestsCommand(user.Id, [.. interests.Select(i => i.Id)], [], "ru");

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
        var (handler, _, _) = CreateHandler(user, simulateConcurrentUpdateConflict: true);
        var command = new PatchUserInterestsCommand(user.Id, [], ["Скалолазание"], "ru");

        await Assert.ThrowsAsync<ProfileUpdateConflictException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА параллельный запрос уже создал кастомный интерес с тем же названием (уникальный индекс) ТОГДА выбрасывается InterestCreationConflictException вместо необработанного исключения")]
    public async Task Handle_translates_a_concurrent_interest_creation_conflict_into_an_interest_creation_conflict()
    {
        var user = NewUser();
        var (handler, _, _) = CreateHandler(user, simulateConcurrentInterestCreationConflict: true);
        var command = new PatchUserInterestsCommand(user.Id, [], ["Скалолазание"], "ru");

        await Assert.ThrowsAsync<InterestCreationConflictException>(() => handler.Handle(command, CancellationToken.None));
    }

    private static (PatchUserInterestsCommandHandler Handler, FakeInterestRepository InterestRepository, FakeSparkTransactionRepository SparkRepository) CreateHandler(
        User user, params Interest[] catalog) =>
        CreateHandler(user, simulateConcurrentUpdateConflict: false, simulateConcurrentInterestCreationConflict: false, catalog);

    private static (PatchUserInterestsCommandHandler Handler, FakeInterestRepository InterestRepository, FakeSparkTransactionRepository SparkRepository) CreateHandler(
        User user, bool simulateConcurrentUpdateConflict = false, bool simulateConcurrentInterestCreationConflict = false, params Interest[] catalog)
    {
        var userRepository = new FakeUserRepository(user, simulateConcurrentUpdateConflict, simulateConcurrentInterestCreationConflict);
        var interestRepository = new FakeInterestRepository(catalog);
        var sparkRepository = new FakeSparkTransactionRepository();
        var sparksService = new SparksService(sparkRepository, userRepository);
        var sparksOptions = Options.Create(new SparksOptions { ProfileCompletionThresholdBonusAmount = 2 });
        var validator = new PatchUserInterestsCommandValidator();

        var handler = new PatchUserInterestsCommandHandler(
            userRepository, interestRepository, new FakeUserDatePreferenceRepository(0), sparksService, validator, sparksOptions);

        return (handler, interestRepository, sparkRepository);
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

    private static Interest NewCatalogInterest(string name, bool isCustom = false) => new()
    {
        Id = Guid.NewGuid(),
        Category = isCustom ? InterestCategory.Custom : InterestCategory.Sport,
        NameRu = name,
        NameBe = name,
        NameEn = name,
        IsCustom = isCustom,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeUserRepository(
        User user, bool simulateConcurrentUpdateConflict = false, bool simulateConcurrentInterestCreationConflict = false) : IUserRepository
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

            if (simulateConcurrentInterestCreationConflict)
            {
                throw new ConcurrentInterestCreationException("Скалолазание", new InvalidOperationException("simulated unique index conflict"));
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeInterestRepository(IEnumerable<Interest> catalog) : IInterestRepository
    {
        private readonly List<Interest> _catalog = [.. catalog];

        public List<Interest> Added { get; } = [];

        public Task<IReadOnlyList<Interest>> GetCatalogAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Interest>>(_catalog);

        public Task<IReadOnlyList<Interest>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Not used by PatchUserInterestsCommandHandler.");

        public Task<IReadOnlyList<Interest>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Interest>>([.. _catalog.Where(i => ids.Contains(i.Id))]);

        public Task<Interest?> FindByNameAsync(string name, CancellationToken cancellationToken) =>
            Task.FromResult(_catalog.FirstOrDefault(i => string.Equals(i.NameRu, name, StringComparison.OrdinalIgnoreCase)));

        public Task AddAsync(Interest interest, CancellationToken cancellationToken)
        {
            _catalog.Add(interest);
            Added.Add(interest);
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
