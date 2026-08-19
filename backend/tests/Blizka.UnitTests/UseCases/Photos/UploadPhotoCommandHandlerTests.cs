using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Blizka.App.UseCases.Photos;
using FluentValidation;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace Blizka.UnitTests.UseCases.Photos;

public sealed class UploadPhotoCommandHandlerTests
{
    private static UploadPhotoCommandHandler CreateHandler(FakePhotoRepository repository, FakePhotoStorageService storage) =>
        new(repository, storage, new UploadPhotoCommandValidator());

    [Fact(DisplayName = "КОГДА пользователь загружает первое фото ТОГДА оно становится главным с SortOrder = 0")]
    public async Task Handle_first_upload_becomes_main_photo_at_sort_order_zero()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePhotoRepository();
        var storage = new FakePhotoStorageService();
        var handler = CreateHandler(repository, storage);
        using var content = CreateJpeg(200, 200);

        var result = await handler.Handle(new UploadPhotoCommand(userId, content, "image/jpeg", content.Length), CancellationToken.None);

        Assert.True(result.IsMain);
        Assert.Equal(0, result.SortOrder);
        var stored = Assert.Single(repository.Photos);
        Assert.Equal(userId, stored.UserId);
        Assert.Equal(3, storage.UploadedKeys.Count);
    }

    [Fact(DisplayName = "КОГДА у пользователя уже есть фото ТОГДА новое не становится главным и получает следующий SortOrder")]
    public async Task Handle_second_upload_is_not_main_and_gets_the_next_sort_order()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePhotoRepository();
        repository.Photos.Add(new Photo { Id = Guid.NewGuid(), UserId = userId, SortOrder = 0, IsMain = true, Url = "u", ThumbnailUrl = "t", MediumUrl = "m" });
        var handler = CreateHandler(repository, new FakePhotoStorageService());
        using var content = CreateJpeg(200, 200);

        var result = await handler.Handle(new UploadPhotoCommand(userId, content, "image/jpeg", content.Length), CancellationToken.None);

        Assert.False(result.IsMain);
        Assert.Equal(1, result.SortOrder);
    }

    [Fact(DisplayName = "КОГДА у пользователя уже 6 фото ТОГДА выбрасывается PhotoLimitExceededException")]
    public async Task Handle_throws_PhotoLimitExceededException_when_the_user_already_has_six_photos()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePhotoRepository();
        for (var i = 0; i < UploadPhotoCommandHandler.MaxPhotosPerUser; i++)
        {
            repository.Photos.Add(new Photo { Id = Guid.NewGuid(), UserId = userId, SortOrder = i, Url = "u", ThumbnailUrl = "t", MediumUrl = "m" });
        }

        var handler = CreateHandler(repository, new FakePhotoStorageService());
        using var content = CreateJpeg(200, 200);

        await Assert.ThrowsAsync<PhotoLimitExceededException>(
            () => handler.Handle(new UploadPhotoCommand(userId, content, "image/jpeg", content.Length), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА Content-Type не поддерживается ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_ValidationException_for_an_unsupported_content_type()
    {
        var handler = CreateHandler(new FakePhotoRepository(), new FakePhotoStorageService());
        using var content = CreateJpeg(200, 200);

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new UploadPhotoCommand(Guid.NewGuid(), content, "text/plain", content.Length), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА размер файла больше 10MB ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_ValidationException_for_a_file_larger_than_10mb()
    {
        var handler = CreateHandler(new FakePhotoRepository(), new FakePhotoStorageService());
        using var content = CreateJpeg(10, 10);
        const long tooLarge = 10 * 1024 * 1024 + 1;

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new UploadPhotoCommand(Guid.NewGuid(), content, "image/jpeg", tooLarge), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА параллельная загрузка уже заняла SortOrder/IsMain (ConcurrentPhotoUploadException) ТОГДА хендлер пересчитывает их по свежему состоянию и повторяет попытку")]
    public async Task Handle_retries_with_a_recomputed_sort_order_after_a_concurrent_upload_conflict()
    {
        var userId = Guid.NewGuid();
        var concurrentPhoto = new Photo { Id = Guid.NewGuid(), UserId = userId, SortOrder = 0, IsMain = true, Url = "u", ThumbnailUrl = "t", MediumUrl = "m" };
        var repository = new FakePhotoRepository
        {
            FailSaveChangesTimes = 1,
            ConcurrentPhotoToInjectOnFirstConflict = concurrentPhoto,
        };
        var handler = CreateHandler(repository, new FakePhotoStorageService());
        using var content = CreateJpeg(200, 200);

        var result = await handler.Handle(new UploadPhotoCommand(userId, content, "image/jpeg", content.Length), CancellationToken.None);

        Assert.Equal(1, result.SortOrder);
        Assert.False(result.IsMain);
        Assert.Equal(2, repository.Photos.Count);
    }

    [Fact(DisplayName = "КОГДА конфликт повторяется MaxConcurrencyAttempts раз подряд ТОГДА выбрасывается PhotoUploadConflictException (409), а не необработанный ConcurrentPhotoUploadException (500)")]
    public async Task Handle_gives_up_after_the_max_number_of_concurrency_attempts()
    {
        var userId = Guid.NewGuid();
        var repository = new FakePhotoRepository { FailSaveChangesTimes = 10 };
        var handler = CreateHandler(repository, new FakePhotoStorageService());
        using var content = CreateJpeg(200, 200);

        await Assert.ThrowsAsync<PhotoUploadConflictException>(
            () => handler.Handle(new UploadPhotoCommand(userId, content, "image/jpeg", content.Length), CancellationToken.None));
    }

    private static MemoryStream CreateJpeg(int width, int height)
    {
        var buffer = new MemoryStream();
        using (var image = new Image<Rgba32>(width, height))
        {
            image.Save(buffer, new JpegEncoder());
        }

        buffer.Position = 0;
        return buffer;
    }

    /// <summary>
    /// Мимикрирует EF Core: <see cref="AddAsync"/> только отслеживает сущность (не видна в <see cref="Photos"/>,
    /// т.е. не учитывается <see cref="CountByUserIdAsync"/>), пока <see cref="SaveChangesAsync"/> не выполнится
    /// успешно — нужно, чтобы тесты на гонку (<see cref="FailSaveChangesTimes"/>) отражали реальную семантику
    /// повторного <c>SaveChangesAsync</c> на том же контексте (см. <c>PhotoRepository</c>).
    /// </summary>
    private sealed class FakePhotoRepository : IPhotoRepository
    {
        private readonly List<Photo> _pending = [];
        private int _saveAttempts;

        public List<Photo> Photos { get; } = [];

        /// <summary>Сколько первых вызовов SaveChangesAsync должны бросить ConcurrentPhotoUploadException.</summary>
        public int FailSaveChangesTimes { get; set; }

        /// <summary>Фото "конкурентной" загрузки — появляется в <see cref="Photos"/> в момент первого смоделированного конфликта.</summary>
        public Photo? ConcurrentPhotoToInjectOnFirstConflict { get; set; }

        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Photos.Count(p => p.UserId == userId));

        public Task<List<Photo>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Photos.Where(p => p.UserId == userId).OrderBy(p => p.SortOrder).ToList());

        public Task AddAsync(Photo photo, CancellationToken cancellationToken)
        {
            _pending.Add(photo);
            return Task.CompletedTask;
        }

        public void Remove(Photo photo)
        {
            Photos.Remove(photo);
            _pending.Remove(photo);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            _saveAttempts++;
            if (_saveAttempts <= FailSaveChangesTimes)
            {
                if (ConcurrentPhotoToInjectOnFirstConflict is { } concurrentPhoto && !Photos.Contains(concurrentPhoto))
                {
                    Photos.Add(concurrentPhoto);
                }

                var userId = _pending.Count > 0 ? _pending[^1].UserId : Guid.Empty;
                throw new ConcurrentPhotoUploadException(userId, new InvalidOperationException("Simulated race for tests."));
            }

            foreach (var photo in _pending.Where(photo => !Photos.Contains(photo)))
            {
                Photos.Add(photo);
            }

            _pending.Clear();
            return Task.CompletedTask;
        }
    }

    private sealed class FakePhotoStorageService : IPhotoStorageService
    {
        public List<string> UploadedKeys { get; } = [];

        public List<string> DeletedKeys { get; } = [];

        public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
        {
            UploadedKeys.Add(key);
            return Task.FromResult($"https://cdn.test/{key}");
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            DeletedKeys.Add(key);
            return Task.CompletedTask;
        }
    }
}
