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
        var handler = new GetFeedQueryHandler(repository, new GetFeedQueryValidator());

        var result = await handler.Handle(new GetFeedQuery(currentUser.Id, 10), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.True(result.Exhausted);
        Assert.False(repository.WasGetCandidatesCalled);
    }

    [Fact(DisplayName = "КОГДА кандидатов в городе нет ТОГДА лента пуста и исчерпана")]
    public async Task Handle_returns_exhausted_empty_feed_when_there_are_no_candidates()
    {
        var currentUser = CreateUser();
        var repository = new FakeFeedRepository { CurrentUser = currentUser, Candidates = [] };
        var handler = new GetFeedQueryHandler(repository, new GetFeedQueryValidator());

        var result = await handler.Handle(new GetFeedQuery(currentUser.Id, 10), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.True(result.Exhausted);
    }

    [Fact(DisplayName = "КОГДА пользователь Male ТОГДА в репозиторий передаётся Female как предпочитаемый пол")]
    public async Task Handle_requests_the_opposite_gender_as_the_default_preference()
    {
        var currentUser = CreateUser(gender: Gender.Male);
        var repository = new FakeFeedRepository { CurrentUser = currentUser, Candidates = [] };
        var handler = new GetFeedQueryHandler(repository, new GetFeedQueryValidator());

        await handler.Handle(new GetFeedQuery(currentUser.Id, 10), CancellationToken.None);

        Assert.Equal(Gender.Female, repository.LastPreferredGender);
        Assert.Equal(currentUser.CityId, repository.LastCityId);
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
        var handler = new GetFeedQueryHandler(repository, new GetFeedQueryValidator());

        var result = await handler.Handle(new GetFeedQuery(currentUser.Id, 1), CancellationToken.None);

        var card = Assert.Single(result.Items);
        Assert.False(result.Exhausted);
        Assert.Equal(bestMatch.Id, card.UserId);
        Assert.True(card.DatingGoalMatch);
        Assert.Equal(1, card.SharedInterestsCount);
        Assert.True(card.BothVerified);
        Assert.Equal(100, card.CompatibilityScore);
    }

    [Fact(DisplayName = "КОГДА у обоих нет ни своих координат, ни города с координатами ТОГДА DistanceKm в карточке null, а не ошибка")]
    public async Task Handle_returns_null_distance_when_neither_user_has_coordinates()
    {
        // CityId задан (иначе лента вернулась бы пустой ещё до подбора кандидатов), а City/Coordinates — нет:
        // ни своей геолокации, ни (в этом искусственном случае) подгруженного города с координатами.
        var currentUser = CreateUser();
        currentUser.Coordinates = null;
        currentUser.City = null;
        var candidate = CreateUser();
        candidate.Coordinates = null;
        candidate.City = null;
        var repository = new FakeFeedRepository { CurrentUser = currentUser, Candidates = [candidate] };
        var handler = new GetFeedQueryHandler(repository, new GetFeedQueryValidator());

        var result = await handler.Handle(new GetFeedQuery(currentUser.Id, 10), CancellationToken.None);

        var card = Assert.Single(result.Items);
        Assert.Null(card.DistanceKm);
    }

    [Fact(DisplayName = "КОГДА limit вне диапазона 1-50 ТОГДА выбрасывается ValidationException и репозиторий не вызывается")]
    public async Task Handle_throws_ValidationException_for_an_out_of_range_limit()
    {
        var repository = new FakeFeedRepository();
        var handler = new GetFeedQueryHandler(repository, new GetFeedQueryValidator());

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

        public Gender? LastPreferredGender { get; private set; }

        public Guid? LastCityId { get; private set; }

        public Task<User?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(CurrentUser);

        public Task<IReadOnlyList<User>> GetCandidatesAsync(
            Guid currentUserId, Guid cityId, Gender preferredGender, int poolSize, CancellationToken cancellationToken)
        {
            WasGetCandidatesCalled = true;
            LastPreferredGender = preferredGender;
            LastCityId = cityId;
            return Task.FromResult(Candidates);
        }
    }
}
