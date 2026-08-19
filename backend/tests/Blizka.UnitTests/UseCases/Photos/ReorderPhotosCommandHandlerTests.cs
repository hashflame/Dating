using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Photos;
using FluentValidation;

namespace Blizka.UnitTests.UseCases.Photos;

public sealed class ReorderPhotosCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА order и mainPhotoId валидны ТОГДА SortOrder пересчитывается по новому порядку, а IsMain выставляется ровно у mainPhotoId")]
    public async Task Handle_applies_the_new_order_and_main_photo()
    {
        var userId = Guid.NewGuid();
        var first = NewPhoto(userId, sortOrder: 0, isMain: true);
        var second = NewPhoto(userId, sortOrder: 1, isMain: false);
        var repository = new FakePhotoRepository([first, second]);
        var handler = new ReorderPhotosCommandHandler(repository);

        var result = await handler.Handle(new ReorderPhotosCommand(userId, [second.Id, first.Id], second.Id), CancellationToken.None);

        Assert.Equal(0, second.SortOrder);
        Assert.True(second.IsMain);
        Assert.Equal(1, first.SortOrder);
        Assert.False(first.IsMain);
        Assert.Equal([second.Id, first.Id], result.Select(p => p.Id));
    }

    [Fact(DisplayName = "КОГДА order не совпадает с текущим набором фото пользователя ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_ValidationException_when_order_does_not_match_the_users_photos()
    {
        var userId = Guid.NewGuid();
        var photo = NewPhoto(userId, sortOrder: 0, isMain: true);
        var repository = new FakePhotoRepository([photo]);
        var handler = new ReorderPhotosCommandHandler(repository);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new ReorderPhotosCommand(userId, [Guid.NewGuid()], photo.Id), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА order содержит повторы ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_ValidationException_when_order_has_duplicates()
    {
        var userId = Guid.NewGuid();
        var first = NewPhoto(userId, sortOrder: 0, isMain: true);
        var second = NewPhoto(userId, sortOrder: 1, isMain: false);
        var repository = new FakePhotoRepository([first, second]);
        var handler = new ReorderPhotosCommandHandler(repository);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new ReorderPhotosCommand(userId, [first.Id, first.Id], first.Id), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА mainPhotoId не входит в order ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_ValidationException_when_main_photo_id_is_not_in_order()
    {
        var userId = Guid.NewGuid();
        var first = NewPhoto(userId, sortOrder: 0, isMain: true);
        var second = NewPhoto(userId, sortOrder: 1, isMain: false);
        var repository = new FakePhotoRepository([first, second]);
        var handler = new ReorderPhotosCommandHandler(repository);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new ReorderPhotosCommand(userId, [first.Id, second.Id], Guid.NewGuid()), CancellationToken.None));
    }

    private static Photo NewPhoto(Guid userId, int sortOrder, bool isMain) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        SortOrder = sortOrder,
        IsMain = isMain,
        Url = "u",
        ThumbnailUrl = "t",
        MediumUrl = "m",
    };

    private sealed class FakePhotoRepository(List<Photo> photos) : IPhotoRepository
    {
        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(photos.Count(p => p.UserId == userId));

        public Task<List<Photo>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(photos.Where(p => p.UserId == userId).OrderBy(p => p.SortOrder).ToList());

        public Task AddAsync(Photo photo, CancellationToken cancellationToken)
        {
            photos.Add(photo);
            return Task.CompletedTask;
        }

        public void Remove(Photo photo) => photos.Remove(photo);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
