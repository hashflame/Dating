using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Blizka.App.Photos;
using MediatR;

namespace Blizka.App.UseCases.Photos;

/// <summary>
/// Удаляет фото пользователя (T-3.1): убирает все три варианта из хранилища и запись из БД. Если удалённое
/// фото было главным, а у пользователя остались другие — главным становится следующее по <c>SortOrder</c>,
/// чтобы профиль не остался без обложки (в задаче явно не описано, но иначе UI получил бы профиль без фото).
/// </summary>
public sealed class DeletePhotoCommandHandler(
    IPhotoRepository photoRepository,
    IPhotoStorageService photoStorageService)
    : IRequestHandler<DeletePhotoCommand>
{
    public async Task Handle(DeletePhotoCommand request, CancellationToken cancellationToken)
    {
        var photos = await photoRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        var photo = photos.SingleOrDefault(p => p.Id == request.PhotoId)
            ?? throw new PhotoNotFoundException(request.PhotoId);

        var keyPrefix = PhotoStorageKeys.Prefix(photo.UserId, photo.Id);
        var originalExtension = PhotoStorageKeys.ExtensionFromUrl(photo.Url);

        await photoStorageService.DeleteAsync(PhotoStorageKeys.Original(keyPrefix, originalExtension), cancellationToken);
        await photoStorageService.DeleteAsync(PhotoStorageKeys.Thumbnail(keyPrefix), cancellationToken);
        await photoStorageService.DeleteAsync(PhotoStorageKeys.Medium(keyPrefix), cancellationToken);

        photoRepository.Remove(photo);

        if (photo.IsMain)
        {
            var newMain = photos
                .Where(p => p.Id != photo.Id)
                .OrderBy(p => p.SortOrder)
                .FirstOrDefault();
            if (newMain is not null)
            {
                newMain.IsMain = true;
            }
        }

        await photoRepository.SaveChangesAsync(cancellationToken);
    }
}
