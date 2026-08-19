using Blizka.App.Domain.Services;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Photos;

/// <summary>
/// Импортирует аватар пользователя из Telegram (T-3.1, <c>POST /api/users/me/photos/import-telegram</c>).
/// <c>photoUrl</c> присылает клиент (значение <c>Telegram.WebApp.initDataUnsafe.user.photo_url</c>) — сервер
/// его не хранит нигде: T-1.1 сознательно не сохраняет photo_url из initData при авторизации (см. заметку
/// в decomposition.md к T-1.1), поэтому на момент импорта актуальное значение есть только у клиента.
/// Скачанный файл прогоняется через тот же MediatR-конвейер, что и обычная загрузка (<see cref="UploadPhotoCommand"/>) —
/// лимит в 6 фото, удаление EXIF и генерация thumbnail/medium общие для обоих путей.
/// </summary>
public sealed class ImportTelegramPhotoCommandHandler(
    ITelegramAvatarDownloader downloader,
    IMediator mediator,
    IValidator<ImportTelegramPhotoCommand> validator)
    : IRequestHandler<ImportTelegramPhotoCommand, PhotoResult>
{
    private const string DefaultContentType = "image/jpeg";

    public async Task<PhotoResult> Handle(ImportTelegramPhotoCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var photoUrl = new Uri(request.PhotoUrl, UriKind.Absolute);
        var download = await downloader.DownloadAsync(photoUrl, cancellationToken);

        // download.Content уже полностью буферизован загрузчиком (см. ITelegramAvatarDownloader) — второй
        // MemoryStream тут не нужен, PhotoImageProcessor.Process сам выставит Position = 0 при декодировании.
        var contentType = string.IsNullOrWhiteSpace(download.ContentType) ? DefaultContentType : download.ContentType;

        try
        {
            return await mediator.Send(
                new UploadPhotoCommand(request.UserId, download.Content, contentType, download.Content.Length),
                cancellationToken);
        }
        finally
        {
            await download.Content.DisposeAsync();
        }
    }
}
