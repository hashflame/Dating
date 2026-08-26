using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Matches;

namespace Blizka.UnitTests.UseCases.Matches;

public sealed class GetDateIdeasQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА у пары есть общее предпочтение на свидания ТОГДА среди идей есть идея под это предпочтение")]
    public async Task Handle_prefers_ideas_matching_a_shared_date_preference()
    {
        var currentUser = CreateUser(preferences: [DatePreferenceCode.QuizzesBoardGames]);
        var other = CreateUser(name: "Anna", preferences: [DatePreferenceCode.QuizzesBoardGames]);
        var match = CreateMatch(currentUser, other);
        var repository = new FakeMatchRepository { ById = match };
        var handler = new GetDateIdeasQueryHandler(repository);

        var result = await handler.Handle(new GetDateIdeasQuery(match.Id, currentUser.Id, "Минск", null, null), CancellationToken.None);

        Assert.InRange(result.Ideas.Count, 2, 3);
        Assert.Contains(result.Ideas, i => i.Title == "Настольные игры в антикафе");
        Assert.All(result.Ideas, i => Assert.Contains("Минск", i.Description));
    }

    [Fact(DisplayName = "КОГДА общих предпочтений нет ТОГДА возвращаются общие идеи без привязки к предпочтению")]
    public async Task Handle_falls_back_to_generic_ideas_without_shared_preferences()
    {
        var currentUser = CreateUser(preferences: [DatePreferenceCode.ActiveOutdoors]);
        var other = CreateUser(name: "Anna", preferences: [DatePreferenceCode.SomethingNew]);
        var match = CreateMatch(currentUser, other);
        var repository = new FakeMatchRepository { ById = match };
        var handler = new GetDateIdeasQueryHandler(repository);

        var result = await handler.Handle(new GetDateIdeasQuery(match.Id, currentUser.Id, null, null, null), CancellationToken.None);

        Assert.InRange(result.Ideas.Count, 2, 3);
    }

    [Fact(DisplayName = "КОГДА указан maxBudget в BYN ТОГДА идеи дороже бюджета не попадают в список")]
    public async Task Handle_filters_ideas_over_budget_when_currency_is_byn()
    {
        var currentUser = CreateUser(preferences: [DatePreferenceCode.SomethingNew]);
        var other = CreateUser(name: "Anna", preferences: [DatePreferenceCode.SomethingNew]);
        var match = CreateMatch(currentUser, other);
        var repository = new FakeMatchRepository { ById = match };
        var handler = new GetDateIdeasQueryHandler(repository);

        var result = await handler.Handle(
            new GetDateIdeasQuery(match.Id, currentUser.Id, null, MaxBudget: 15m, Currency: "BYN"), CancellationToken.None);

        Assert.All(result.Ideas, i => Assert.True(i.EstimatedCost <= 15m));
    }

    [Fact(DisplayName = "КОГДА запрошена валюта отличная от BYN ТОГДА ответ всё равно подписан BYN, а не запрошенной валютой")]
    public async Task Handle_always_labels_the_response_as_byn_regardless_of_requested_currency()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var repository = new FakeMatchRepository { ById = match };
        var handler = new GetDateIdeasQueryHandler(repository);

        var result = await handler.Handle(
            new GetDateIdeasQuery(match.Id, currentUser.Id, null, MaxBudget: 12m, Currency: "USD"), CancellationToken.None);

        // Фильтр по бюджету применяется только для BYN — при USD он пропускается, но подпись валюты в ответе
        // не должна выдавать BYN-цифру за доллары (найдено на ревью, T-12.1).
        Assert.All(result.Ideas, i => Assert.Equal("BYN", i.Currency));
        Assert.Contains(result.Ideas, i => i.EstimatedCost > 12m);
    }

    [Fact(DisplayName = "КОГДА мэтча с таким id нет для этого пользователя ТОГДА выбрасывается MatchNotFoundException")]
    public async Task Handle_throws_when_the_match_is_not_found_for_the_requesting_user()
    {
        var repository = new FakeMatchRepository { ById = null };
        var handler = new GetDateIdeasQueryHandler(repository);

        await Assert.ThrowsAsync<MatchNotFoundException>(
            () => handler.Handle(new GetDateIdeasQuery(Guid.NewGuid(), Guid.NewGuid(), null, null, null), CancellationToken.None));
    }

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

    private static User CreateUser(string name = "Me", IReadOnlyList<DatePreferenceCode>? preferences = null)
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            TelegramId = Random.Shared.NextInt64(),
            Status = UserStatus.Active,
            Name = name,
            BirthDate = new DateOnly(1995, 1, 1),
            Gender = Gender.Female,
            Locale = "ru",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        };

        foreach (var code in preferences ?? [])
        {
            var datePreference = new DatePreference { Id = Guid.NewGuid(), Code = code };
            user.UserDatePreferences.Add(
                new UserDatePreference { UserId = userId, DatePreferenceId = datePreference.Id, DatePreference = datePreference });
        }

        return user;
    }

    private sealed class FakeMatchRepository : IMatchRepository
    {
        public Match? ById { get; set; }

        public Task AddAsync(Match match, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах идей свидания.");

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах идей свидания.");

        public void Remove(Match match) =>
            throw new NotSupportedException("Не используется в тестах идей свидания.");

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах идей свидания.");

        public Task<int> CountNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах идей свидания.");

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах идей свидания.");

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах идей свидания.");

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken)
        {
            var found = ById is not null && ById.Id == matchId && (ById.User1Id == userId || ById.User2Id == userId)
                ? ById
                : null;
            return Task.FromResult(found);
        }

        public Task<Match?> GetByIdForUserBasicAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах идей свидания.");

        public Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах идей свидания.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах идей свидания.");

        public Task RemoveAllForUserAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("RemoveAllForUserAsync не используется в этом тесте.");

        public Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах идей свидания.");
    }
}
