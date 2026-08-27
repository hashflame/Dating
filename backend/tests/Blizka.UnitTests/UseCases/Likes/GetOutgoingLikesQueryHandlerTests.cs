using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Likes;

namespace Blizka.UnitTests.UseCases.Likes;

public sealed class GetOutgoingLikesQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА есть исходящие лайки без мэтча ТОГДА возвращается их count и полный список")]
    public async Task Handle_returns_the_full_outgoing_list()
    {
        var likedUser = CreateUser("Anna");
        likedUser.Photos.Add(new Photo
        {
            Id = Guid.NewGuid(), UserId = likedUser.Id, Url = "u", ThumbnailUrl = "t", MediumUrl = "m", IsMain = true,
        });
        var likesRepository = new FakeLikesRepository { Outgoing = [new LikeEntry(likedUser, DateTimeOffset.UtcNow)] };
        var handler = new GetOutgoingLikesQueryHandler(likesRepository, new FakePrivacySettingsRepository());

        var result = await handler.Handle(new GetOutgoingLikesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(1, result.Count);
        Assert.Single(result.Users);
        Assert.Equal("Anna", result.Users[0].Name);
        Assert.Equal("u", result.Users[0].MainPhotoUrl);
    }

    [Fact(DisplayName = "КОГДА среди исходящих лайков уже есть мэтч ТОГДА он остаётся в списке с IsMatched и MatchId, а не исчезает")]
    public async Task Handle_marks_a_matched_entry_instead_of_excluding_it()
    {
        var likedUser = CreateUser("Anna");
        var matchId = Guid.NewGuid();
        var likesRepository = new FakeLikesRepository { Outgoing = [new LikeEntry(likedUser, DateTimeOffset.UtcNow, matchId)] };
        var handler = new GetOutgoingLikesQueryHandler(likesRepository, new FakePrivacySettingsRepository());

        var result = await handler.Handle(new GetOutgoingLikesQuery(Guid.NewGuid()), CancellationToken.None);

        var user = Assert.Single(result.Users);
        Assert.True(user.IsMatched);
        Assert.Equal(matchId, user.MatchId);
    }

    [Fact(DisplayName = "КОГДА исходящих лайков нет ТОГДА возвращается пустой список с count 0")]
    public async Task Handle_returns_an_empty_list_when_there_are_no_outgoing_likes()
    {
        var likesRepository = new FakeLikesRepository();
        var handler = new GetOutgoingLikesQueryHandler(likesRepository, new FakePrivacySettingsRepository());

        var result = await handler.Handle(new GetOutgoingLikesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(0, result.Count);
        Assert.Empty(result.Users);
    }

    private static User CreateUser(string name) => new()
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

    private sealed class FakeLikesRepository : ILikesRepository
    {
        public IReadOnlyList<LikeEntry> Outgoing { get; set; } = [];

        public Task<int> CountIncomingAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах исходящих лайков.");

        public Task<IReadOnlyList<LikeEntry>> GetIncomingPreviewAsync(Guid userId, int limit, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах исходящих лайков.");

        public Task<IReadOnlyList<LikeEntry>> GetIncomingAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах исходящих лайков.");

        public Task<IReadOnlyList<LikeEntry>> GetOutgoingAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Outgoing);
    }

    private sealed class FakePrivacySettingsRepository : IPrivacySettingsRepository
    {
        public Dictionary<Guid, PrivacySettings> ByUserId { get; } = [];

        public Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(ByUserId.GetValueOrDefault(userId));

        public Task<PrivacySettings?> GetByUserIdTrackedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах исходящих лайков.");

        public Task<IReadOnlyDictionary<Guid, PrivacySettings>> GetByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, PrivacySettings>>(
                ByUserId.Where(kv => userIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));

        public Task AddAsync(PrivacySettings settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах исходящих лайков.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах исходящих лайков.");
    }
}
