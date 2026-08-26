using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Matches;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace Blizka.UnitTests.UseCases.Matches;

public sealed class GetMatchHubQueryHandlerTests
{
    private static readonly GeometryFactory GeometryFactory = new(new PrecisionModel(), 4326);

    [Fact(DisplayName = "КОГДА контакт ещё не открыт ТОГДА contactStatus locked и telegramUsername скрыт")]
    public async Task Handle_returns_locked_status_and_hides_telegram_username_before_unlock()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna", telegramUsername: "anna_k");
        var match = CreateMatch(currentUser, other);
        var repository = new FakeMatchRepository { ById = match };
        var privacyRepository = new FakePrivacySettingsRepository();
        var handler = new GetMatchHubQueryHandler(repository, privacyRepository, CreateSparksOptions(contactUnlockCost: 1));

        var result = await handler.Handle(new GetMatchHubQuery(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Equal("locked", result.ContactStatus);
        Assert.Null(result.User.TelegramUsername);
        Assert.Equal(1, result.ContactCost);
        Assert.Equal("Anna", result.User.Name);
    }

    [Fact(DisplayName = "КОГДА контакт открыт ТОГДА contactStatus unlocked и telegramUsername виден")]
    public async Task Handle_returns_unlocked_status_and_reveals_telegram_username_after_unlock()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna", telegramUsername: "anna_k");
        var match = CreateMatch(currentUser, other);
        match.ContactUnlockedAt = DateTimeOffset.UtcNow;
        var repository = new FakeMatchRepository { ById = match };
        var privacyRepository = new FakePrivacySettingsRepository();
        var handler = new GetMatchHubQueryHandler(repository, privacyRepository, CreateSparksOptions());

        var result = await handler.Handle(new GetMatchHubQuery(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Equal("unlocked", result.ContactStatus);
        Assert.Equal("anna_k", result.User.TelegramUsername);
    }

    [Fact(DisplayName = "КОГДА есть общие интересы и совпадает цель знакомства ТОГДА details перечисляет их")]
    public async Task Handle_builds_details_from_shared_interests_and_dating_goal()
    {
        var sharedInterest = CreateInterest("Кино");
        var currentUser = CreateUser(datingGoal: DatingGoal.LongTermRelationship, interests: [sharedInterest]);
        var other = CreateUser(
            name: "Anna", datingGoal: DatingGoal.LongTermRelationship, interests: [sharedInterest], coordinates: currentUser.Coordinates);
        var match = CreateMatch(currentUser, other);
        var repository = new FakeMatchRepository { ById = match };
        var privacyRepository = new FakePrivacySettingsRepository();
        var handler = new GetMatchHubQueryHandler(repository, privacyRepository, CreateSparksOptions());

        var result = await handler.Handle(new GetMatchHubQuery(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Contains("Кино", result.Compatibility.Details);
        Assert.Contains("Совпадает цель знакомства", result.Compatibility.Details);
    }

    [Fact(DisplayName = "КОГДА фичи ещё не реализованы ТОГДА Minigame/StaleConversation available: false, а QuestionOfDay (T-11.1) и DateIdea (T-12.1) — true")]
    public async Task Handle_stubs_remaining_feature_branches_as_unavailable()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var repository = new FakeMatchRepository { ById = match };
        var privacyRepository = new FakePrivacySettingsRepository();
        var handler = new GetMatchHubQueryHandler(repository, privacyRepository, CreateSparksOptions());

        var result = await handler.Handle(new GetMatchHubQuery(match.Id, currentUser.Id), CancellationToken.None);

        Assert.True(result.Features.QuestionOfDay.Available);
        Assert.False(result.Features.Minigame.Available);
        Assert.True(result.Features.DateIdea.Available);
        Assert.False(result.Features.StaleConversation.Available);
    }

    [Fact(DisplayName = "КОГДА у второго участника включён blockIncomingMessages и контакт не открыт ТОГДА contactStatus writes_first_only (T-16.1)")]
    public async Task Handle_returns_writes_first_only_when_the_other_participant_blocks_incoming_messages()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna", telegramUsername: "anna_k");
        var match = CreateMatch(currentUser, other);
        var repository = new FakeMatchRepository { ById = match };
        var privacyRepository = new FakePrivacySettingsRepository();
        privacyRepository.ByUserId[other.Id] = new PrivacySettings { UserId = other.Id, BlockIncomingMessages = true };
        var handler = new GetMatchHubQueryHandler(repository, privacyRepository, CreateSparksOptions());

        var result = await handler.Handle(new GetMatchHubQuery(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Equal("writes_first_only", result.ContactStatus);
    }

    [Fact(DisplayName = "КОГДА у второго участника выключен showLastActive ТОГДА lastActiveAt в хабе скрыт (T-16.1)")]
    public async Task Handle_hides_last_active_when_the_other_participant_disabled_showing_it()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        other.LastActiveAt = DateTimeOffset.UtcNow;
        var match = CreateMatch(currentUser, other);
        var repository = new FakeMatchRepository { ById = match };
        var privacyRepository = new FakePrivacySettingsRepository();
        privacyRepository.ByUserId[other.Id] = new PrivacySettings { UserId = other.Id, ShowLastActive = false };
        var handler = new GetMatchHubQueryHandler(repository, privacyRepository, CreateSparksOptions());

        var result = await handler.Handle(new GetMatchHubQuery(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Null(result.User.LastActiveAt);
    }

    [Fact(DisplayName = "КОГДА мэтча с таким id нет для этого пользователя ТОГДА выбрасывается MatchNotFoundException")]
    public async Task Handle_throws_when_the_match_is_not_found_for_the_requesting_user()
    {
        var repository = new FakeMatchRepository { ById = null };
        var privacyRepository = new FakePrivacySettingsRepository();
        var handler = new GetMatchHubQueryHandler(repository, privacyRepository, CreateSparksOptions());

        await Assert.ThrowsAsync<MatchNotFoundException>(
            () => handler.Handle(new GetMatchHubQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    private static IOptions<SparksOptions> CreateSparksOptions(int contactUnlockCost = 1) =>
        Options.Create(new SparksOptions { ContactUnlockCost = contactUnlockCost });

    private static Match CreateMatch(User currentUser, User other)
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
            MatchedAt = DateTimeOffset.UtcNow,
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
        string name = "Me",
        DatingGoal? datingGoal = null,
        Point? coordinates = null,
        string? telegramUsername = null,
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
            TelegramUsername = telegramUsername,
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
        public Match? ById { get; set; }

        public Task AddAsync(Match match, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public void Remove(Match match) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public Task<int> CountNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken)
        {
            var found = ById is not null && ById.Id == matchId && (ById.User1Id == userId || ById.User2Id == userId)
                ? ById
                : null;
            return Task.FromResult(found);
        }

        public Task<Match?> GetByIdForUserBasicAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");
    }

    private sealed class FakePrivacySettingsRepository : IPrivacySettingsRepository
    {
        public Dictionary<Guid, PrivacySettings> ByUserId { get; } = [];

        public Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(ByUserId.GetValueOrDefault(userId));

        public Task<PrivacySettings?> GetByUserIdTrackedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public Task<IReadOnlyDictionary<Guid, PrivacySettings>> GetByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public Task AddAsync(PrivacySettings settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах хаба мэтча.");
    }
}
