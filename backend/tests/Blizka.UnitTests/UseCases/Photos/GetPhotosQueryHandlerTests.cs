using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Photos;

namespace Blizka.UnitTests.UseCases.Photos;

public sealed class GetPhotosQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА у пользователя есть фото ТОГДА они возвращаются в порядке SortOrder")]
    public async Task Handle_returns_photos_in_sort_order()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePhotoRepository();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        repository.Photos.Add(new Photo { Id = secondId, UserId = userId, SortOrder = 1, IsMain = false, Url = "u2", ThumbnailUrl = "t2", MediumUrl = "m2" });
        repository.Photos.Add(new Photo { Id = firstId, UserId = userId, SortOrder = 0, IsMain = true, Url = "u1", ThumbnailUrl = "t1", MediumUrl = "m1" });
        var handler = new GetPhotosQueryHandler(repository);

        var result = await handler.Handle(new GetPhotosQuery(userId), CancellationToken.None);

        Assert.Equal([firstId, secondId], result.Select(p => p.Id));
        Assert.True(result[0].IsMain);
    }

    [Fact(DisplayName = "КОГДА у пользователя нет фото ТОГДА возвращается пустой список")]
    public async Task Handle_returns_empty_list_when_no_photos()
    {
        var handler = new GetPhotosQueryHandler(new FakePhotoRepository());

        var result = await handler.Handle(new GetPhotosQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Empty(result);
    }

    private sealed class FakePhotoRepository : IPhotoRepository
    {
        public List<Photo> Photos { get; } = [];

        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Подсчёт фото не используется в тестах чтения списка.");

        public Task<List<Photo>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Photos.Where(p => p.UserId == userId).OrderBy(p => p.SortOrder).ToList());

        public Task AddAsync(Photo photo, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Добавление фото не используется в тестах чтения списка.");

        public void Remove(Photo photo) =>
            throw new NotSupportedException("Удаление фото не используется в тестах чтения списка.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Сохранение не используется в тестах чтения списка.");
    }
}
