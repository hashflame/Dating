using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Feed;
using FluentValidation;
using NetTopologySuite.Geometries;

namespace Blizka.UnitTests.UseCases.Feed;

public sealed class GetFeedQueryHandlerTests
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    [Fact(DisplayName = "КОГДА у пользователя не задан город ТОГДА лента пуста и исчерпана, кандидаты не запрашиваются")]
    public async Task Handle_returns_exhausted_empty_feed_when_the_user_has_no_city()
    {
        var currentUser = CreateUser(hasCity: false);
        var repository = new FakeFeedRepository { CurrentUser = currentUser };
        var handler = new GetFeedQueryHandler(repository, new FakeUserFilterRepository(), new FakeSwipeRepository(), new GetFeedQueryValidator());

        var result = await handler.Handle(new GetFeedQuery(currentUser.Id, 10), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.True(result.Exhausted);
        Assert.False(repository.WasGetCandidatesCalled);
    }

    [Fact(DisplayName = "КОГДА у пользователя есть город, но нигде нет координат ТОГДА лента пуста и исчерпана, кандидаты не запрашиваются")]
    public async Task Handle_returns_exhausted_empty_feed_when_no_origin_coordinates_are_resolvable()
    {
        // Практически недостижимо для Active-пользователя (GetCurrentUserAsync всегда грузит City, а
        // City.Coordinates не nullable) — подстраховка на будущее, как и в FeedCompatibilityScorer.
        var currentUser = CreateUser();
        currentUser.Coordinates = null;
        currentUser.City = null;
        var repository = new FakeFeedRepository { CurrentUser = currentUser };
        var handler = new GetFeedQueryHandler(repository, new FakeUserFilterRepository(), new FakeSwipeRepository(), new GetFeedQueryValidator());

        var result = await handler.Handle(new GetFeedQuery(currentUser.Id, 10), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.True(result.Exhausted);
        Assert.False(repository.WasGetCandidatesCalled);
    }

    [Fact(DisplayName = "КОГДА кандидатов в радиусе нет ТОГДА лента пуста и исчерпана")]
    public async Task Handle_returns_exhausted_empty_feed_when_there_are_no_candidates()
    {
        var currentUser = CreateUser();
        var repository = new FakeFeedRepository { CurrentUser = currentUser, Candidates = [] };
        var handler = new GetFeedQueryHandler(repository, new FakeUserFilterRepository(), new FakeSwipeRepository(), new GetFeedQueryValidator());

        var result = await handler.Handle(new GetFeedQuery(currentUser.Id, 10), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.True(result.Exhausted);
    }

    [Fact(DisplayName = "КОГДА нет сохранённого UserFilter и пользователь Male ТОГДА в фильтре передаётся Female и MVP-дефолт радиуса")]
    public async Task Handle_requests_the_opposite_gender_and_default_radius_when_no_filter_is_saved()
    {
        var currentUser = CreateUser(gender: Gender.Male);
        var repository = new FakeFeedRepository { CurrentUser = currentUser, Candidates = [] };
        var handler = new GetFeedQueryHandler(repository, new FakeUserFilterRepository(), new FakeSwipeRepository(), new GetFeedQueryValidator());

        await handler.Handle(new GetFeedQuery(currentUser.Id, 10), CancellationToken.None);

        Assert.NotNull(repository.LastFilter);
        Assert.Equal(Gender.Female, repository.LastFilter!.PreferredGender);
        Assert.Equal(UserFilterDefaults.MaxDistanceKm * 1000.0, repository.LastFilter.MaxDistanceMeters);
        // spec.md §6.1 — фото по умолчанию обязательны, пока пользователь не сохранил свои фильтры (spec 002, B5).
        Assert.True(repository.LastFilter.RequirePhoto);
    }

    [Fact(DisplayName = "КОГДА сохранён UserFilter с ShowGender=All ТОГДА в фильтре PreferredGender не задан, а радиус — свой")]
    public async Task Handle_uses_the_saved_filter_instead_of_mvp_defaults()
    {
        var currentUser = CreateUser(gender: Gender.Male);
        var savedFilter = new UserFilter
        {
            UserId = currentUser.Id,
            ShowGender = ShowGenderPreference.All,
            AgeMin = 20,
            AgeMax = 30,
            MaxDistanceKm = 15,
            DatingGoals = [DatingGoal.Casual],
        };
        var repository = new FakeFeedRepository { CurrentUser = currentUser, Candidates = [] };
        var filterRepository = new FakeUserFilterRepository { Filter = savedFilter };
        var handler = new GetFeedQueryHandler(repository, filterRepository, new FakeSwipeRepository(), new GetFeedQueryValidator());

        await handler.Handle(new GetFeedQuery(currentUser.Id, 10), CancellationToken.None);

        Assert.NotNull(repository.LastFilter);
        Assert.Null(repository.LastFilter!.PreferredGender);
        Assert.Equal(15 * 1000.0, repository.LastFilter.MaxDistanceMeters);
        Assert.Equal(20, repository.LastFilter.AgeMin);
        Assert.Equal(30, repository.LastFilter.AgeMax);
        Assert.Equal([DatingGoal.Casual], repository.LastFilter.DatingGoals);
    }

    [Fact(DisplayName = "КОГДА кандидаты есть ТОГДА карточки отсортированы по убыванию совместимости и обрезаны по limit")]
    public async Task Handle_returns_cards_sorted_by_score_descending_and_capped_at_limit()
    {
        var sharedInterest = CreateInterest("Кино");
        var currentUser = CreateUser(datingGoal: DatingGoal.LongTermRelationship, interests: [sharedInterest]);

        // Полное совпадение: цель + интерес + совместные координаты (0 км) + оба верифицированы.
        var bestMatch = CreateUser(
            name: "Best",
            datingGoal: DatingGoal.LongTermRelationship,
            interests: [sharedInterest],
            isVerified: true,
            coordinates: currentUser.Coordinates);
        currentUser.IsVerified = true;

        // Ничего не совпадает и координаты далеко.
        var worstMatch = CreateUser(
            name: "Worst",
            datingGoal: DatingGoal.Friendship,
            interests: [],
            coordinates: GeometryFactory.CreatePoint(new Coordinate(50, 50)));

        var repository = new FakeFeedRepository { CurrentUser = currentUser, Candidates = [worstMatch, bestMatch] };
        var handler = new GetFeedQueryHandler(repository, new FakeUserFilterRepository(), new FakeSwipeRepository(), new GetFeedQueryValidator());

        var result = await handler.Handle(new GetFeedQuery(currentUser.Id, 1), CancellationToken.None);

        var card = Assert.Single(result.Items);
        Assert.False(result.Exhausted);
        Assert.Equal(bestMatch.Id, card.UserId);
        Assert.True(card.DatingGoalMatch);
        Assert.Equal(1, card.SharedInterestsCount);
        Assert.True(card.BothVerified);
        Assert.Equal(100, card.CompatibilityScore);
        // Прокидывание существующих полей User без новой бизнес-логики (spec 002, B12).
        Assert.Equal(DatingGoal.LongTermRelationship, card.DatingGoal);
        Assert.Equal(bestMatch.LastActiveAt, card.LastActive);
    }

    [Fact(DisplayName = "КОГДА у кандидата нет ни своих координат, ни города с координатами ТОГДА DistanceKm в карточке null, а не ошибка")]
    public async Task Handle_returns_null_distance_when_the_candidate_has_no_coordinates()
    {
        var currentUser = CreateUser();
        var candidate = CreateUser();
        candidate.Coordinates = null;
        candidate.City = null;
        var repository = new FakeFeedRepository { CurrentUser = currentUser, Candidates = [candidate] };
        var handler = new GetFeedQueryHandler(repository, new FakeUserFilterRepository(), new FakeSwipeRepository(), new GetFeedQueryValidator());

        var result = await handler.Handle(new GetFeedQuery(currentUser.Id, 10), CancellationToken.None);

        var card = Assert.Single(result.Items);
        Assert.Null(card.DistanceKm);
    }

    [Fact(DisplayName = "КОГДА пользователь сделал N свайпов за 24 часа ТОГДА remainingToday = 50 - N (spec 002, B3)")]
    public async Task Handle_returns_the_remaining_daily_swipe_count()
    {
        var currentUser = CreateUser();
        var repository = new FakeFeedRepository { CurrentUser = currentUser, Candidates = [] };
        var swipeRepository = new FakeSwipeRepository { SwipesUsedToday = 12 };
        var handler = new GetFeedQueryHandler(repository, new FakeUserFilterRepository(), swipeRepository, new GetFeedQueryValidator());

        var result = await handler.Handle(new GetFeedQuery(currentUser.Id, 10), CancellationToken.None);

        Assert.Equal(38, result.RemainingToday);
    }

    [Fact(DisplayName = "КОГДА пользователь уже исчерпал дневной лимит ТОГДА remainingToday = 0, а не отрицательное число")]
    public async Task Handle_clamps_remaining_swipes_to_zero()
    {
        var currentUser = CreateUser(hasCity: false);
        var repository = new FakeFeedRepository { CurrentUser = currentUser };
        var swipeRepository = new FakeSwipeRepository { SwipesUsedToday = 60 };
        var handler = new GetFeedQueryHandler(repository, new FakeUserFilterRepository(), swipeRepository, new GetFeedQueryValidator());

        var result = await handler.Handle(new GetFeedQuery(currentUser.Id, 10), CancellationToken.None);

        Assert.Equal(0, result.RemainingToday);
    }

    [Fact(DisplayName = "КОГДА limit вне диапазона 1-50 ТОГДА выбрасывается ValidationException и репозиторий не вызывается")]
    public async Task Handle_throws_ValidationException_for_an_out_of_range_limit()
    {
        var repository = new FakeFeedRepository();
        var handler = new GetFeedQueryHandler(repository, new FakeUserFilterRepository(), new FakeSwipeRepository(), new GetFeedQueryValidator());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new GetFeedQuery(Guid.NewGuid(), 0), CancellationToken.None));
        Assert.False(repository.WasGetCandidatesCalled);
    }

    private static Interest CreateInterest(string nameRu) => new()
    {
        Id = Guid.NewGuid(),
        Category = InterestCategory.Entertainment,
        NameRu = nameRu,
        NameBe = nameRu,
        NameEn = nameRu,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static User CreateUser(
        string name = "Anna",
        Gender gender = Gender.Female,
        bool hasCity = true,
        DatingGoal? datingGoal = null,
        bool isVerified = false,
        Point? coordinates = null,
        IReadOnlyList<Interest>? interests = null)
    {
        var userId = Guid.NewGuid();
        City? city = null;
        Guid? cityId = null;
        if (hasCity)
        {
            cityId = Guid.NewGuid();
            city = new City
            {
                Id = cityId.Value,
                NameRu = "Минск",
                NameBe = "Мінск",
                NameEn = "Minsk",
                Country = "BY",
                Coordinates = GeometryFactory.CreatePoint(new Coordinate(27.5667, 53.9)),
                IsOpen = true,
                CreatedAt = DateTimeOffset.UtcNow,
            };
        }

        var user = new User
        {
            Id = userId,
            TelegramId = Random.Shared.NextInt64(),
            Status = UserStatus.Active,
            Name = name,
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
            Gender = gender,
            CityId = cityId,
            City = city,
            Coordinates = coordinates ?? GeometryFactory.CreatePoint(new Coordinate(27.5667, 53.9)),
            DatingGoal = datingGoal,
            IsVerified = isVerified,
            Locale = "ru",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        foreach (var interest in interests ?? [])
        {
            user.UserInterests.Add(new UserInterest { UserId = userId, InterestId = interest.Id, Interest = interest });
        }

        return user;
    }

    private sealed class FakeFeedRepository : IFeedRepository
    {
        public User? CurrentUser { get; set; }

        public IReadOnlyList<User> Candidates { get; set; } = [];

        public bool WasGetCandidatesCalled { get; private set; }

        public FeedCandidateFilter? LastFilter { get; private set; }

        public Task<User?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(CurrentUser);

        public Task<IReadOnlyList<User>> GetCandidatesAsync(
            Guid currentUserId, FeedCandidateFilter filter, int poolSize, CancellationToken cancellationToken)
        {
            WasGetCandidatesCalled = true;
            LastFilter = filter;
            return Task.FromResult(Candidates);
        }
    }

    private sealed class FakeSwipeRepository : ISwipeRepository
    {
        public int SwipesUsedToday { get; set; }

        public Task<bool> ExistsActiveAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ленты.");

        public Task<bool> HasActiveMutualLikeAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ленты.");

        public Task<Swipe?> GetLastActiveAsync(Guid fromUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ленты.");

        public Task<int> CountUndoneSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ленты.");

        public Task<int> CountSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            Task.FromResult(SwipesUsedToday);

        public Task<DateTimeOffset?> GetOldestCreatedAtSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ленты.");

        public Task AddAsync(Swipe swipe, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ленты.");

        public Task RemoveAllByUserAsync(Guid fromUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ленты.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ленты.");
    }

    private sealed class FakeUserFilterRepository : IUserFilterRepository
    {
        public UserFilter? Filter { get; set; }

        public Task<UserFilter?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Filter);

        public Task AddAsync(UserFilter filter, CancellationToken cancellationToken)
        {
            Filter = filter;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
