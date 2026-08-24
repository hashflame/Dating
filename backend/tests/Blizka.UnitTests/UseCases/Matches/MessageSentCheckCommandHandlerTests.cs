using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Matches;

namespace Blizka.UnitTests.UseCases.Matches;

public sealed class MessageSentCheckCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА MessageSentCheckAt ещё не проставлен ТОГДА хендлер проставляет его и сохраняет")]
    public async Task Handle_sets_the_timestamp_on_the_first_call()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var repository = new FakeMatchRepository { ById = match };
        var handler = new MessageSentCheckCommandHandler(repository);

        await handler.Handle(new MessageSentCheckCommand(match.Id, currentUser.Id), CancellationToken.None);

        Assert.NotNull(match.MessageSentCheckAt);
        Assert.True(repository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА MessageSentCheckAt уже проставлен ТОГДА повторный вызов не сдвигает момент и не сохраняет заново")]
    public async Task Handle_is_idempotent_when_already_set()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var originalTimestamp = DateTimeOffset.UtcNow.AddDays(-1);
        match.MessageSentCheckAt = originalTimestamp;
        var repository = new FakeMatchRepository { ById = match };
        var handler = new MessageSentCheckCommandHandler(repository);

        await handler.Handle(new MessageSentCheckCommand(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Equal(originalTimestamp, match.MessageSentCheckAt);
        Assert.False(repository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА мэтча с таким id нет для этого пользователя ТОГДА выбрасывается MatchNotFoundException")]
    public async Task Handle_throws_when_the_match_is_not_found_for_the_requesting_user()
    {
        var repository = new FakeMatchRepository { ById = null };
        var handler = new MessageSentCheckCommandHandler(repository);

        await Assert.ThrowsAsync<MatchNotFoundException>(
            () => handler.Handle(new MessageSentCheckCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
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
            throw new NotSupportedException("Не используется в тестах message-sent-check.");

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах message-sent-check.");

        public void Remove(Match match) =>
            throw new NotSupportedException("Не используется в тестах message-sent-check.");

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах message-sent-check.");

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах message-sent-check.");

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах message-sent-check.");

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах message-sent-check.");

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
    }
}
