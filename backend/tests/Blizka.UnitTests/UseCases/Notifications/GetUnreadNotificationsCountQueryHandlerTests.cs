using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Notifications;

namespace Blizka.UnitTests.UseCases.Notifications;

public sealed class GetUnreadNotificationsCountQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА у пользователя нет LastSeenLikesAt/LastSeenMatchesAt ТОГДА считаются все входящие лайки и новые мэтчи")]
    public async Task Handle_returns_the_full_counts_when_nothing_was_marked_seen_yet()
    {
        var user = CreateUser();
        var likesRepository = new FakeLikesRepository { IncomingCount = 3 };
        var matchRepository = new FakeMatchRepository { NewCount = 2 };
        var handler = new GetUnreadNotificationsCountQueryHandler(likesRepository, matchRepository, new FakeUserRepository(user));

        var result = await handler.Handle(new GetUnreadNotificationsCountQuery(user.Id), CancellationToken.None);

        Assert.Equal(3, result.Likes);
        Assert.Equal(2, result.Matches);
        Assert.Equal(user.Id, likesRepository.RequestedUserId);
        Assert.Equal(user.Id, matchRepository.RequestedUserId);
        Assert.Null(likesRepository.RequestedSince);
        Assert.Null(matchRepository.RequestedSince);
    }

    [Fact(DisplayName = "КОГДА ничего непрочитанного нет ТОГДА оба счётчика равны нулю")]
    public async Task Handle_returns_zero_when_there_is_nothing_unread()
    {
        var user = CreateUser();
        var handler = new GetUnreadNotificationsCountQueryHandler(new FakeLikesRepository(), new FakeMatchRepository(), new FakeUserRepository(user));

        var result = await handler.Handle(new GetUnreadNotificationsCountQuery(user.Id), CancellationToken.None);

        Assert.Equal(0, result.Likes);
        Assert.Equal(0, result.Matches);
    }

    [Fact(DisplayName = "КОГДА пользователь погасил бейджи (LastSeenLikesAt/LastSeenMatchesAt выставлены) ТОГДА обе метки передаются в репозитории для фильтрации по времени (тикет ClickUp: бейдж было невозможно погасить)")]
    public async Task Handle_passes_the_last_seen_marks_to_the_repositories()
    {
        var lastSeenLikesAt = DateTimeOffset.UtcNow.AddHours(-2);
        var lastSeenMatchesAt = DateTimeOffset.UtcNow.AddHours(-1);
        var user = CreateUser();
        user.LastSeenLikesAt = lastSeenLikesAt;
        user.LastSeenMatchesAt = lastSeenMatchesAt;
        var likesRepository = new FakeLikesRepository { IncomingCount = 1 };
        var matchRepository = new FakeMatchRepository { NewCount = 1 };
        var handler = new GetUnreadNotificationsCountQueryHandler(likesRepository, matchRepository, new FakeUserRepository(user));

        await handler.Handle(new GetUnreadNotificationsCountQuery(user.Id), CancellationToken.None);

        Assert.Equal(lastSeenLikesAt, likesRepository.RequestedSince);
        Assert.Equal(lastSeenMatchesAt, matchRepository.RequestedSince);
    }

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = "Ann",
        BirthDate = new DateOnly(1995, 1, 1),
        Gender = Gender.Female,
        Locale = "ru",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeUserRepository(User user) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id == id ? user : null);

        public Task AddAsync(User newUser, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");
    }

    private sealed class FakeLikesRepository : ILikesRepository
    {
        public int IncomingCount { get; set; }

        public Guid RequestedUserId { get; private set; }

        public DateTimeOffset? RequestedSince { get; private set; }

        public Task<int> CountIncomingAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task<int> CountIncomingSinceAsync(Guid userId, DateTimeOffset? since, CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
            RequestedSince = since;
            return Task.FromResult(IncomingCount);
        }

        public Task<IReadOnlyList<LikeEntry>> GetIncomingPreviewAsync(Guid userId, int limit, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task<IReadOnlyList<LikeEntry>> GetIncomingAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task<IReadOnlyList<LikeEntry>> GetOutgoingAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");
    }

    private sealed class FakeMatchRepository : IMatchRepository
    {
        public int NewCount { get; set; }

        public Guid RequestedUserId { get; private set; }

        public DateTimeOffset? RequestedSince { get; private set; }

        public Task<int> CountNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task<int> CountNewSinceAsync(Guid userId, DateTimeOffset? since, CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
            RequestedSince = since;
            return Task.FromResult(NewCount);
        }

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task AddAsync(Match match, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public void Remove(Match match) => throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task<Match?> GetByIdForUserBasicAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");

        public Task RemoveAllForUserAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("RemoveAllForUserAsync не используется в этом тесте.");

        public Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");
    }
}
