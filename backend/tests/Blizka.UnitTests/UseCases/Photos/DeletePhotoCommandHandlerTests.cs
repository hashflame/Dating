using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Blizka.App.UseCases.Photos;

namespace Blizka.UnitTests.UseCases.Photos;

public sealed class DeletePhotoCommandHandlerTests
{
    private static DeletePhotoCommandHandler CreateHandler(FakePhotoRepository repository, FakePhotoStorageService storage) =>
        new(repository, storage);

    [Fact(DisplayName = "КОГДА фото существует ТОГДА оно удаляется из репозитория и все три варианта — из хранилища")]
    public async Task Handle_removes_the_photo_and_all_three_storage_variants()
    {
        var userId = Guid.NewGuid();
        var photoId = Guid.NewGuid();
        var repository = new FakePhotoRepository();
        repository.Photos.Add(new Photo
        {
            Id = photoId,
            UserId = userId,
            SortOrder = 0,
            IsMain = true,
            Url = $"https://cdn.test/photos/{userId:N}/{photoId:N}/original.png",
            ThumbnailUrl = "t",
            MediumUrl = "m",
        });
        var storage = new FakePhotoStorageService();
        var handler = CreateHandler(repository, storage);

        await handler.Handle(new DeletePhotoCommand(userId, photoId), CancellationToken.None);

        Assert.Empty(repository.Photos);
        Assert.Equal(
            [$"photos/{userId:N}/{photoId:N}/original.png", $"photos/{userId:N}/{photoId:N}/thumbnail.jpg", $"photos/{userId:N}/{photoId:N}/medium.jpg"],
            storage.DeletedKeys);
    }

    [Fact(DisplayName = "КОГДА фото не найдено (в т.ч. принадлежит другому пользователю) ТОГДА выбрасывается PhotoNotFoundException")]
    public async Task Handle_throws_PhotoNotFoundException_when_the_photo_belongs_to_another_user()
    {
        var repository = new FakePhotoRepository();
        repository.Photos.Add(new Photo { Id = Guid.NewGuid(), UserId = Guid.NewGuid(), Url = "u", ThumbnailUrl = "t", MediumUrl = "m" });
        var handler = CreateHandler(repository, new FakePhotoStorageService());

        await Assert.ThrowsAsync<PhotoNotFoundException>(
            () => handler.Handle(new DeletePhotoCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА удаляется главное фото, а другие остаются ТОГДА главным становится следующее по SortOrder")]
    public async Task Handle_promotes_the_next_photo_to_main_when_the_main_photo_is_deleted()
    {
        var userId = Guid.NewGuid();
        var mainPhotoId = Guid.NewGuid();
        var otherPhotoId = Guid.NewGuid();
        var repository = new FakePhotoRepository();
        repository.Photos.Add(new Photo { Id = mainPhotoId, UserId = userId, SortOrder = 0, IsMain = true, Url = "u1", ThumbnailUrl = "t1", MediumUrl = "m1" });
        repository.Photos.Add(new Photo { Id = otherPhotoId, UserId = userId, SortOrder = 1, IsMain = false, Url = "u2", ThumbnailUrl = "t2", MediumUrl = "m2" });
        var handler = CreateHandler(repository, new FakePhotoStorageService());

        await handler.Handle(new DeletePhotoCommand(userId, mainPhotoId), CancellationToken.None);

        var remaining = Assert.Single(repository.Photos);
        Assert.Equal(otherPhotoId, remaining.Id);
        Assert.True(remaining.IsMain);
    }

    [Fact(DisplayName = "КОГДА удаляется не главное фото ТОГДА текущее главное фото не меняется")]
    public async Task Handle_does_not_change_the_main_photo_when_deleting_a_non_main_photo()
    {
        var userId = Guid.NewGuid();
        var mainPhotoId = Guid.NewGuid();
        var otherPhotoId = Guid.NewGuid();
        var repository = new FakePhotoRepository();
        repository.Photos.Add(new Photo { Id = mainPhotoId, UserId = userId, SortOrder = 0, IsMain = true, Url = "u1", ThumbnailUrl = "t1", MediumUrl = "m1" });
        repository.Photos.Add(new Photo { Id = otherPhotoId, UserId = userId, SortOrder = 1, IsMain = false, Url = "u2", ThumbnailUrl = "t2", MediumUrl = "m2" });
        var handler = CreateHandler(repository, new FakePhotoStorageService());

        await handler.Handle(new DeletePhotoCommand(userId, otherPhotoId), CancellationToken.None);

        var remaining = Assert.Single(repository.Photos);
        Assert.Equal(mainPhotoId, remaining.Id);
        Assert.True(remaining.IsMain);
    }

    private sealed class FakePhotoRepository : IPhotoRepository
    {
        public List<Photo> Photos { get; } = [];

        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Photos.Count(p => p.UserId == userId));

        public Task<List<Photo>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Photos.Where(p => p.UserId == userId).OrderBy(p => p.SortOrder).ToList());

        public Task AddAsync(Photo photo, CancellationToken cancellationToken)
        {
            Photos.Add(photo);
            return Task.CompletedTask;
        }

        public void Remove(Photo photo) => Photos.Remove(photo);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakePhotoStorageService : IPhotoStorageService
    {
        public List<string> DeletedKeys { get; } = [];

        public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Upload не ожидается в сценариях удаления.");

        public Task<byte[]> DownloadAsync(string key, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Download не ожидается в сценариях удаления.");

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            DeletedKeys.Add(key);
            return Task.CompletedTask;
        }

        public Task<string> GetTemporaryDownloadUrlAsync(string key, TimeSpan validFor, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не ожидается в сценариях удаления.");
    }
}
