using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Blizka.App.Photos;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Photos;

/// <summary>
/// Загружает фото профиля (T-3.1): декодирует, снимает EXIF, генерирует thumbnail/medium и заливает все три
/// варианта в S3-совместимое хранилище. Первое загруженное фото пользователя автоматически становится главным.
/// </summary>
public sealed class UploadPhotoCommandHandler(
    IPhotoRepository photoRepository,
    IPhotoStorageService photoStorageService,
    IValidator<UploadPhotoCommand> validator)
    : IRequestHandler<UploadPhotoCommand, PhotoResult>
{
    public const int MaxPhotosPerUser = 6;

    private const int MaxConcurrencyAttempts = 3;

    public async Task<PhotoResult> Handle(UploadPhotoCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var existingCount = await photoRepository.CountByUserIdAsync(request.UserId, cancellationToken);
        if (existingCount >= MaxPhotosPerUser)
        {
            throw new PhotoLimitExceededException(request.UserId, MaxPhotosPerUser);
        }

        var processed = PhotoImageProcessor.Process(request.Content);

        var photoId = Guid.NewGuid();
        var keyPrefix = PhotoStorageKeys.Prefix(request.UserId, photoId);

        var url = await photoStorageService.UploadAsync(
            PhotoStorageKeys.Original(keyPrefix, processed.OriginalExtension),
            new MemoryStream(processed.OriginalBytes),
            processed.OriginalContentType,
            cancellationToken);
        var thumbnailUrl = await photoStorageService.UploadAsync(
            PhotoStorageKeys.Thumbnail(keyPrefix),
            new MemoryStream(processed.ThumbnailBytes),
            "image/jpeg",
            cancellationToken);
        var mediumUrl = await photoStorageService.UploadAsync(
            PhotoStorageKeys.Medium(keyPrefix),
            new MemoryStream(processed.MediumBytes),
            "image/jpeg",
            cancellationToken);

        var photo = new Photo
        {
            Id = photoId,
            UserId = request.UserId,
            Url = url,
            ThumbnailUrl = thumbnailUrl,
            MediumUrl = mediumUrl,
            SortOrder = existingCount,
            IsMain = existingCount == 0,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await photoRepository.AddAsync(photo, cancellationToken);
        await SaveWithRetryAsync(photoRepository, photo, request.UserId, cancellationToken);

        return ToResult(photo);
    }

    /// <summary>
    /// Два параллельных <c>POST /photos</c> для одного пользователя (например, двойной тап на медленной
    /// сети) могут прочитать одинаковый <c>existingCount</c> и столкнуться на уникальных индексах
    /// (UserId, SortOrder)/(UserId, IsMain) — см. <see cref="ConcurrentPhotoUploadException"/>. Проигравший
    /// запрос пересчитывает актуальные SortOrder/IsMain по свежему состоянию БД и повторяет попытку
    /// (сущность уже отслеживается контекстом после неудачного SaveChangesAsync — новый AddAsync не нужен).
    /// </summary>
    private static async Task SaveWithRetryAsync(
        IPhotoRepository photoRepository, Photo photo, Guid userId, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await photoRepository.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (ConcurrentPhotoUploadException) when (attempt < MaxConcurrencyAttempts)
            {
                var currentCount = await photoRepository.CountByUserIdAsync(userId, cancellationToken);
                if (currentCount >= MaxPhotosPerUser)
                {
                    throw new PhotoLimitExceededException(userId, MaxPhotosPerUser);
                }

                photo.SortOrder = currentCount;
                photo.IsMain = currentCount == 0;
            }
            catch (ConcurrentPhotoUploadException ex)
            {
                // Бюджет попыток исчерпан (маловероятно вне искусственного стресс-теста — на практике хватает
                // и одного повтора на "двойной тап") — файл уже в хранилище, но занять место в БД не вышло.
                // Переводим внутренний recovery-сигнал в обычный BlizkaDomainException, а не даём ему утечь
                // в API-слой необработанным (иначе клиент получил бы 500 вместо понятного "повторите запрос").
                throw new PhotoUploadConflictException(userId, ex);
            }
        }
    }

    internal static PhotoResult ToResult(Photo photo) =>
        new(photo.Id, photo.Url, photo.ThumbnailUrl, photo.MediumUrl, photo.SortOrder, photo.IsMain, photo.CreatedAt);
}
