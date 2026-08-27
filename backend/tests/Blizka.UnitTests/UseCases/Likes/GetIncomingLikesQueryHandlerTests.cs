using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Likes;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace Blizka.UnitTests.UseCases.Likes;

public sealed class GetIncomingLikesQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА список ещё не разблокирован ТОГДА возвращаются count и заблюренные превью, без users")]
    public async Task Handle_returns_blurred_preview_when_not_revealed()
    {
        var user = CreateUser(likesRevealed: false);
        var liker = CreateUser();
        liker.Photos.Add(CreatePhoto(liker.Id, isMain: true));
        var handler = CreateHandler(out var likesRepository, out var photoStorage, users: [user]);
        likesRepository.IncomingCount = 3;
        likesRepository.IncomingPreview = [new LikeEntry(liker, DateTimeOffset.UtcNow)];

        var result = await handler.Handle(new GetIncomingLikesQuery(user.Id), CancellationToken.None);

        Assert.Equal(3, result.Count);
        Assert.False(result.Revealed);
        Assert.Equal(10, result.UnlockCost);
        Assert.Single(result.BlurredPreviewPhotos);
        Assert.Empty(result.Users);
        Assert.Single(photoStorage.RequestedKeys);
    }

    [Fact(DisplayName = "КОГДА у лайкнувшего в превью нет фото ТОГДА он пропускается, а не падает")]
    public async Task Handle_skips_preview_entries_without_photos()
    {
        var user = CreateUser(likesRevealed: false);
        var likerWithoutPhoto = CreateUser();
        var handler = CreateHandler(out var likesRepository, out var photoStorage, users: [user]);
        likesRepository.IncomingPreview = [new LikeEntry(likerWithoutPhoto, DateTimeOffset.UtcNow)];

        var result = await handler.Handle(new GetIncomingLikesQuery(user.Id), CancellationToken.None);

        Assert.Empty(result.BlurredPreviewPhotos);
        Assert.Empty(photoStorage.RequestedKeys);
    }

    [Fact(DisplayName = "КОГДА скачивание/блюр thumbnail падает ТОГДА эта запись пропускается, а не 500 на весь список")]
    public async Task Handle_skips_a_preview_entry_when_the_download_fails()
    {
        var user = CreateUser(likesRevealed: false);
        var liker = CreateUser();
        liker.Photos.Add(CreatePhoto(liker.Id, isMain: true));
        var handler = CreateHandler(out var likesRepository, out var photoStorage, users: [user]);
        likesRepository.IncomingCount = 1;
        likesRepository.IncomingPreview = [new LikeEntry(liker, DateTimeOffset.UtcNow)];
        photoStorage.FailDownloads = true;

        var result = await handler.Handle(new GetIncomingLikesQuery(user.Id), CancellationToken.None);

        Assert.Equal(1, result.Count);
        Assert.Empty(result.BlurredPreviewPhotos);
    }

    [Fact(DisplayName = "КОГДА список уже разблокирован ТОГДА возвращается полный список users без загрузки фото")]
    public async Task Handle_returns_the_full_list_when_already_revealed()
    {
        var user = CreateUser(likesRevealed: true);
        var liker = CreateUser(name: "Anna");
        liker.Photos.Add(CreatePhoto(liker.Id, isMain: true));
        var handler = CreateHandler(out var likesRepository, out var photoStorage, users: [user]);
        likesRepository.Incoming = [new LikeEntry(liker, DateTimeOffset.UtcNow)];
        likesRepository.IncomingCount = 1;

        var result = await handler.Handle(new GetIncomingLikesQuery(user.Id), CancellationToken.None);

        Assert.True(result.Revealed);
        Assert.Equal(1, result.Count);
        Assert.Empty(result.BlurredPreviewPhotos);
        Assert.Single(result.Users);
        Assert.Equal("Anna", result.Users[0].Name);
        Assert.Empty(photoStorage.RequestedKeys);
    }

    private static GetIncomingLikesQueryHandler CreateHandler(
        out FakeLikesRepository likesRepository, out FakePhotoStorageService photoStorage, IReadOnlyList<User> users)
    {
        var userRepository = new FakeUserRepository(users);
        likesRepository = new FakeLikesRepository();
        photoStorage = new FakePhotoStorageService();
        var options = Options.Create(new SparksOptions { LikesRevealCost = 10 });

        return new GetIncomingLikesQueryHandler(
            userRepository, likesRepository, new FakePrivacySettingsRepository(), photoStorage, options);
    }

    private static User CreateUser(string name = "User", bool likesRevealed = false) => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = name,
        BirthDate = new DateOnly(1995, 1, 1),
        Gender = Gender.Female,
        Locale = "ru",
        LikesRevealed = likesRevealed,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static Photo CreatePhoto(Guid userId, bool isMain) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Url = "https://cdn.test/original.jpg",
        ThumbnailUrl = "https://cdn.test/thumbnail.jpg",
        MediumUrl = "https://cdn.test/medium.jpg",
        IsMain = isMain,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static byte[] CreateJpegBytes()
    {
        using var buffer = new MemoryStream();
        using (var image = new Image<Rgba32>(10, 10))
        {
            image.Save(buffer, new JpegEncoder());
        }

        return buffer.ToArray();
    }

    private sealed class FakeUserRepository(IReadOnlyList<User> users) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");
    }

    private sealed class FakeLikesRepository : ILikesRepository
    {
        public int IncomingCount { get; set; }

        public IReadOnlyList<LikeEntry> IncomingPreview { get; set; } = [];

        public IReadOnlyList<LikeEntry> Incoming { get; set; } = [];

        public IReadOnlyList<LikeEntry> Outgoing { get; set; } = [];

        public Task<int> CountIncomingAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(IncomingCount);

        public Task<IReadOnlyList<LikeEntry>> GetIncomingPreviewAsync(Guid userId, int limit, CancellationToken cancellationToken) =>
            Task.FromResult(IncomingPreview);

        public Task<IReadOnlyList<LikeEntry>> GetIncomingAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Incoming);

        public Task<IReadOnlyList<LikeEntry>> GetOutgoingAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Outgoing);
    }

    private sealed class FakePhotoStorageService : IPhotoStorageService
    {
        public List<string> RequestedKeys { get; } = [];

        public bool FailDownloads { get; set; }

        public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");

        public Task<byte[]> DownloadAsync(string key, CancellationToken cancellationToken)
        {
            RequestedKeys.Add(key);
            if (FailDownloads)
            {
                throw new InvalidOperationException("simulated storage failure");
            }

            return Task.FromResult(CreateJpegBytes());
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");

        public Task<string> GetTemporaryDownloadUrlAsync(string key, TimeSpan validFor, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");
    }

    private sealed class FakePrivacySettingsRepository : IPrivacySettingsRepository
    {
        public Dictionary<Guid, PrivacySettings> ByUserId { get; } = [];

        public Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(ByUserId.GetValueOrDefault(userId));

        public Task<PrivacySettings?> GetByUserIdTrackedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");

        public Task<IReadOnlyDictionary<Guid, PrivacySettings>> GetByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, PrivacySettings>>(
                ByUserId.Where(kv => userIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));

        public Task AddAsync(PrivacySettings settings, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списков лайков.");
    }
}
