using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Notifications;

namespace Blizka.UnitTests.UseCases.Notifications;

public sealed class GetUnreadNotificationsCountQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА есть входящие лайки и новые мэтчи ТОГДА оба счётчика возвращаются как есть")]
    public async Task Handle_returns_the_counts_from_both_repositories()
    {
        var userId = Guid.NewGuid();
        var likesRepository = new FakeLikesRepository { IncomingCount = 3 };
        var matchRepository = new FakeMatchRepository { NewCount = 2 };
        var handler = new GetUnreadNotificationsCountQueryHandler(likesRepository, matchRepository);

        var result = await handler.Handle(new GetUnreadNotificationsCountQuery(userId), CancellationToken.None);

        Assert.Equal(3, result.Likes);
        Assert.Equal(2, result.Matches);
        Assert.Equal(userId, likesRepository.RequestedUserId);
        Assert.Equal(userId, matchRepository.RequestedUserId);
    }

    [Fact(DisplayName = "КОГДА ничего непрочитанного нет ТОГДА оба счётчика равны нулю")]
    public async Task Handle_returns_zero_when_there_is_nothing_unread()
    {
        var handler = new GetUnreadNotificationsCountQueryHandler(new FakeLikesRepository(), new FakeMatchRepository());

        var result = await handler.Handle(new GetUnreadNotificationsCountQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(0, result.Likes);
        Assert.Equal(0, result.Matches);
    }

    private sealed class FakeLikesRepository : ILikesRepository
    {
        public int IncomingCount { get; set; }

        public Guid RequestedUserId { get; private set; }

        public Task<int> CountIncomingAsync(Guid userId, CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
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

        public Task<int> CountNewAsync(Guid userId, CancellationToken cancellationToken)
        {
            RequestedUserId = userId;
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

        public Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах счётчика уведомлений.");
    }
}
