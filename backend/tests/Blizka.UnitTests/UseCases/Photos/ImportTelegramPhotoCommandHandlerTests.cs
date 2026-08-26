using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Blizka.App.UseCases.Photos;
using FluentValidation;
using MediatR;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace Blizka.UnitTests.UseCases.Photos;

public sealed class ImportTelegramPhotoCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА photoUrl — ссылка на Telegram CDN ТОГДА файл скачивается и проходит через тот же конвейер, что и обычная загрузка")]
    public async Task Handle_downloads_and_uploads_the_avatar_through_the_upload_pipeline()
    {
        var userId = Guid.NewGuid();
        var photoRepository = new FakePhotoRepository();
        var photoStorage = new FakePhotoStorageService();
        var downloader = new FakeTelegramAvatarDownloader(CreateJpeg(200, 200), "image/jpeg");
        var uploadHandler = new UploadPhotoCommandHandler(photoRepository, photoStorage, new UploadPhotoCommandValidator());
        var mediator = new SingleHandlerMediator(uploadHandler);
        var handler = new ImportTelegramPhotoCommandHandler(downloader, mediator, new ImportTelegramPhotoCommandValidator());

        var result = await handler.Handle(new ImportTelegramPhotoCommand(userId, "https://t.me/i/userpic/320/abc.jpg"), CancellationToken.None);

        Assert.True(result.IsMain);
        Assert.Equal("https://t.me/i/userpic/320/abc.jpg", downloader.RequestedUrl!.ToString());
        Assert.Single(photoRepository.Photos);
    }

    [Fact(DisplayName = "КОГДА photoUrl указывает не на Telegram CDN ТОГДА выбрасывается ValidationException и файл не скачивается")]
    public async Task Handle_throws_ValidationException_for_a_non_telegram_host_without_downloading()
    {
        var downloader = new FakeTelegramAvatarDownloader(CreateJpeg(10, 10), "image/jpeg");
        var handler = new ImportTelegramPhotoCommandHandler(
            downloader,
            new SingleHandlerMediator(new UploadPhotoCommandHandler(new FakePhotoRepository(), new FakePhotoStorageService(), new UploadPhotoCommandValidator())),
            new ImportTelegramPhotoCommandValidator());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new ImportTelegramPhotoCommand(Guid.NewGuid(), "https://evil.example.com/x.jpg"), CancellationToken.None));
        Assert.False(downloader.WasCalled);
    }

    [Fact(DisplayName = "КОГДА photoUrl использует http (не https) ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_ValidationException_for_a_non_https_url()
    {
        var downloader = new FakeTelegramAvatarDownloader(CreateJpeg(10, 10), "image/jpeg");
        var handler = new ImportTelegramPhotoCommandHandler(
            downloader,
            new SingleHandlerMediator(new UploadPhotoCommandHandler(new FakePhotoRepository(), new FakePhotoStorageService(), new UploadPhotoCommandValidator())),
            new ImportTelegramPhotoCommandValidator());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new ImportTelegramPhotoCommand(Guid.NewGuid(), "http://t.me/i/userpic/320/abc.jpg"), CancellationToken.None));
        Assert.False(downloader.WasCalled);
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

    private sealed class FakeTelegramAvatarDownloader(Stream content, string? contentType) : ITelegramAvatarDownloader
    {
        public bool WasCalled { get; private set; }

        public Uri? RequestedUrl { get; private set; }

        public Task<TelegramAvatarDownload> DownloadAsync(Uri photoUrl, CancellationToken cancellationToken)
        {
            WasCalled = true;
            RequestedUrl = photoUrl;
            return Task.FromResult(new TelegramAvatarDownload(content, contentType));
        }
    }

    /// <summary>Форвардит только <c>Send&lt;TResponse&gt;(IRequest&lt;TResponse&gt;)</c> — единственный член IMediator, используемый хендлером импорта.</summary>
    private sealed class SingleHandlerMediator(UploadPhotoCommandHandler uploadHandler) : IMediator
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is UploadPhotoCommand command)
            {
                return (Task<TResponse>)(object)uploadHandler.Handle(command, cancellationToken);
            }

            throw new NotSupportedException($"Unexpected request type {request.GetType()}.");
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest =>
            throw new NotSupportedException();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish(object notification, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification =>
            throw new NotSupportedException();
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
        public Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken) =>
            Task.FromResult($"https://cdn.test/{key}");

        public Task<byte[]> DownloadAsync(string key, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Download не ожидается в этих тестах.");

        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<string> GetTemporaryDownloadUrlAsync(string key, TimeSpan validFor, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не ожидается в этих тестах.");
    }
}
