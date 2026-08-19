namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда несколько параллельных загрузок фото для одного пользователя сталкивались на
/// SortOrder/IsMain больше <c>UploadPhotoCommandHandler.MaxConcurrencyAttempts</c> раз подряд — сам файл
/// уже загружен в хранилище, но занять свободную позицию не удалось. Клиенту стоит просто повторить запрос.
/// </summary>
public sealed class PhotoUploadConflictException(Guid userId, Exception innerException)
    : BlizkaDomainException(
        "PHOTO_UPLOAD_CONFLICT",
        $"Photo upload for user {userId} conflicted with other concurrent uploads too many times.",
        new Dictionary<string, object?> { ["userId"] = userId },
        innerException)
{
    public Guid UserId { get; } = userId;
}
