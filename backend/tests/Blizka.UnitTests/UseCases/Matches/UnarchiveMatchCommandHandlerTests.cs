using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Matches;

namespace Blizka.UnitTests.UseCases.Matches;

public sealed class UnarchiveMatchCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА мэтч заархивирован ТОГДА хендлер возвращает его в Active и очищает ArchivedAt")]
    public async Task Handle_restores_an_archived_match()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        match.Status = MatchStatus.Archived;
        match.ArchivedAt = DateTimeOffset.UtcNow.AddDays(-1);
        match.ArchivedReason = MatchArchivalPolicy.ManualArchivedReason;
        var repository = new FakeMatchRepository { ById = match };
        var handler = new UnarchiveMatchCommandHandler(repository);

        await handler.Handle(new UnarchiveMatchCommand(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Equal(MatchStatus.Active, match.Status);
        Assert.Null(match.ArchivedAt);
        Assert.Null(match.ArchivedReason);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА мэтч уже активен ТОГДА повторный вызов ничего не меняет и не сохраняет заново")]
    public async Task Handle_is_idempotent_when_already_active()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var repository = new FakeMatchRepository { ById = match };
        var handler = new UnarchiveMatchCommandHandler(repository);

        await handler.Handle(new UnarchiveMatchCommand(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Equal(MatchStatus.Active, match.Status);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА мэтча с таким id нет для этого пользователя ТОГДА выбрасывается MatchNotFoundException")]
    public async Task Handle_throws_when_the_match_is_not_found_for_the_requesting_user()
    {
        var repository = new FakeMatchRepository { ById = null };
        var handler = new UnarchiveMatchCommandHandler(repository);

        await Assert.ThrowsAsync<MatchNotFoundException>(
            () => handler.Handle(new UnarchiveMatchCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    private static User CreateUser(string name = "Me") => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = name,
        BirthDate = new DateOnly(1995, 1, 1),
        Gender = Gender.Female,
        Locale = "ru",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

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

    private sealed class FakeMatchRepository : IMatchRepository
    {
        public Match? ById { get; set; }

        public bool SaveChangesCalled { get; private set; }

        public Task AddAsync(Match match, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах восстановления мэтча.");

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах восстановления мэтча.");

        public void Remove(Match match) =>
            throw new NotSupportedException("Не используется в тестах восстановления мэтча.");

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах восстановления мэтча.");

        public Task<int> CountNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах восстановления мэтча.");

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах восстановления мэтча.");

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах восстановления мэтча.");

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах восстановления мэтча.");

        public Task<Match?> GetByIdForUserBasicAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах восстановления мэтча.");

        public Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken)
        {
            var found = ById is not null && ById.Id == matchId && (ById.User1Id == userId || ById.User2Id == userId)
                ? ById
                : null;
            return Task.FromResult(found);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }

        public Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах восстановления мэтча.");
    }
}
