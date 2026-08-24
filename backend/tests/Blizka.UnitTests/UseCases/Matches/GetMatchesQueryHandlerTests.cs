using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Matches;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace Blizka.UnitTests.UseCases.Matches;

public sealed class GetMatchesQueryHandlerTests
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    [Fact(DisplayName = "КОГДА оба пользователя максимально совместимы ТОГДА в секции new бейдж fire и полная стоимость контакта")]
    public async Task Handle_sets_the_fire_badge_for_a_highly_compatible_new_match()
    {
        var sharedInterest = CreateInterest("Кино");
        var currentUser = CreateUser(datingGoal: DatingGoal.LongTermRelationship, interests: [sharedInterest], isVerified: true);
        var other = CreateUser(
            name: "Best",
            datingGoal: DatingGoal.LongTermRelationship,
            interests: [sharedInterest],
            isVerified: true,
            coordinates: currentUser.Coordinates);
        var match = CreateMatch(currentUser, other, matchedAt: DateTimeOffset.UtcNow);
        var repository = new FakeMatchRepository { New = [match] };
        var handler = new GetMatchesQueryHandler(repository, CreateSparksOptions(contactUnlockCost: 1));

        var result = await handler.Handle(new GetMatchesQuery(currentUser.Id), CancellationToken.None);

        var item = Assert.Single(result.New);
        Assert.Equal(other.Id, item.User.UserId);
        Assert.Equal("fire", item.Badge);
        Assert.Equal(1, item.ContactCost);
        Assert.False(item.WritesFirst);
    }

    [Fact(DisplayName = "КОГДА пользователи ничем не совпадают ТОГДА в секции new бейдж отсутствует")]
    public async Task Handle_leaves_the_badge_null_for_a_low_compatibility_new_match()
    {
        var currentUser = CreateUser(datingGoal: DatingGoal.LongTermRelationship);
        var other = CreateUser(
            name: "Worst",
            datingGoal: DatingGoal.Friendship,
            coordinates: GeometryFactory.CreatePoint(new Coordinate(50, 50)));
        var match = CreateMatch(currentUser, other, matchedAt: DateTimeOffset.UtcNow);
        var repository = new FakeMatchRepository { New = [match] };
        var handler = new GetMatchesQueryHandler(repository, CreateSparksOptions());

        var result = await handler.Handle(new GetMatchesQuery(currentUser.Id), CancellationToken.None);

        var item = Assert.Single(result.New);
        Assert.Null(item.Badge);
    }

    [Fact(DisplayName = "КОГДА второй участник — User1 канонизированной пары ТОГДА всё равно резолвится как other, а не как я")]
    public async Task Handle_resolves_the_other_participant_even_when_the_current_user_is_user2()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Other");
        // Канонизация в MatchRepository кладёт меньший Id в User1 — здесь явно строим обратный порядок.
        var match = new Match
        {
            Id = Guid.NewGuid(),
            User1Id = other.Id,
            User1 = other,
            User2Id = currentUser.Id,
            User2 = currentUser,
            Status = MatchStatus.Active,
            MatchedAt = DateTimeOffset.UtcNow,
        };
        var repository = new FakeMatchRepository { New = [match] };
        var handler = new GetMatchesQueryHandler(repository, CreateSparksOptions());

        var result = await handler.Handle(new GetMatchesQuery(currentUser.Id), CancellationToken.None);

        var item = Assert.Single(result.New);
        Assert.Equal(other.Id, item.User.UserId);
    }

    [Fact(DisplayName = "КОГДА мэтч ждёт сообщения ТОГДА бейдж contact_opened и contactOpenedAt = ContactUnlockedAt")]
    public async Task Handle_returns_the_contact_opened_badge_for_waiting_matches()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Vera");
        var unlockedAt = DateTimeOffset.UtcNow.AddDays(-2);
        var match = CreateMatch(currentUser, other, matchedAt: DateTimeOffset.UtcNow.AddDays(-3));
        match.ContactUnlockedAt = unlockedAt;
        var repository = new FakeMatchRepository { WaitingForMessage = [match] };
        var handler = new GetMatchesQueryHandler(repository, CreateSparksOptions());

        var result = await handler.Handle(new GetMatchesQuery(currentUser.Id), CancellationToken.None);

        var item = Assert.Single(result.WaitingForMessage);
        Assert.Equal("contact_opened", item.Badge);
        Assert.Equal(unlockedAt, item.ContactOpenedAt);
    }

    [Fact(DisplayName = "КОГДА у заархивированного мэтча есть ArchivedReason ТОГДА он возвращается как есть, без эвристики")]
    public async Task Handle_returns_the_persisted_reason_verbatim()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Nika");
        var matchedAt = DateTimeOffset.UtcNow.AddDays(-10);
        var match = CreateMatch(currentUser, other, matchedAt: matchedAt);
        match.Status = MatchStatus.Archived;
        match.ArchivedAt = matchedAt.AddDays(1);
        match.ArchivedReason = "manual";
        var repository = new FakeMatchRepository { Archived = [match] };
        var handler = new GetMatchesQueryHandler(repository, CreateSparksOptions());

        var result = await handler.Handle(new GetMatchesQuery(currentUser.Id), CancellationToken.None);

        var item = Assert.Single(result.Archived);
        // Регрессия на баг, найденный в code review: мэтч заархивирован вручную на 1-й день (ArchivedReason
        // = "manual" проставлен тогда же), а MatchedAt уже 10 дней назад — старая эвристика на момент чтения
        // ошибочно переквалифицировала бы такой мэтч в "no_activity_7_days" задним числом.
        Assert.Equal("manual", item.Reason);
        Assert.Equal(matchedAt.AddDays(1), item.ArchivedAt);
    }

    [Fact(DisplayName = "КОГДА у заархивированного мэтча нет ArchivedReason (легаси-данные) ТОГДА причина вычисляется эвристикой по MatchArchivalPolicy")]
    public async Task Handle_falls_back_to_the_staleness_heuristic_when_reason_is_missing()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Nika");
        var matchedAt = DateTimeOffset.UtcNow.AddDays(-10);
        var match = CreateMatch(currentUser, other, matchedAt: matchedAt);
        match.Status = MatchStatus.Archived;
        var repository = new FakeMatchRepository { Archived = [match] };
        var handler = new GetMatchesQueryHandler(repository, CreateSparksOptions());

        var result = await handler.Handle(new GetMatchesQuery(currentUser.Id), CancellationToken.None);

        var item = Assert.Single(result.Archived);
        Assert.Equal("no_activity_7_days", item.Reason);
        Assert.Equal(matchedAt, item.ArchivedAt);
    }

    private static IOptions<SparksOptions> CreateSparksOptions(int contactUnlockCost = 1) =>
        Options.Create(new SparksOptions { ContactUnlockCost = contactUnlockCost });

    private static Match CreateMatch(User currentUser, User other, DateTimeOffset matchedAt)
    {
        var (user1, user2) = currentUser.Id.CompareTo(other.Id) < 0 ? (currentUser, other) : (other, currentUser);
        return new Match
        {
            Id = Guid.NewGuid(),
            User1Id = user1.Id,
            User1 = user1,
            User2Id = user2.Id,
            User2 = user2,
            Status = MatchStatus.Active,
            MatchedAt = matchedAt,
        };
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
        DatingGoal? datingGoal = null,
        bool isVerified = false,
        Point? coordinates = null,
        IReadOnlyList<Interest>? interests = null)
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TelegramId = Random.Shared.NextInt64(),
            Status = UserStatus.Active,
            Name = name,
            BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
            Gender = Gender.Female,
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

    private sealed class FakeMatchRepository : IMatchRepository
    {
        public IReadOnlyList<Match> New { get; set; } = [];

        public IReadOnlyList<Match> WaitingForMessage { get; set; } = [];

        public IReadOnlyList<Match> Archived { get; set; } = [];

        public Match? ById { get; set; }

        public Task AddAsync(Match match, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списка мэтчей.");

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списка мэтчей.");

        public void Remove(Match match) =>
            throw new NotSupportedException("Не используется в тестах списка мэтчей.");

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(New);

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(WaitingForMessage);

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Archived);

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken)
        {
            var found = ById is not null && ById.Id == matchId && (ById.User1Id == userId || ById.User2Id == userId)
                ? ById
                : null;
            return Task.FromResult(found);
        }

        public Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списка мэтчей.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списка мэтчей.");

        public Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списка мэтчей.");
    }
}
