namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда фото не найдено — в том числе когда оно принадлежит другому пользователю
/// (запрос всегда ищет фото в паре (photoId, userId), так что чужие фото не отличимы от несуществующих — IDOR-защита).
/// </summary>
public sealed class PhotoNotFoundException(Guid photoId)
    : BlizkaDomainException(
        "PHOTO_NOT_FOUND",
        $"Photo {photoId} was not found.",
        new Dictionary<string, object?> { ["photoId"] = photoId })
{
    public Guid PhotoId { get; } = photoId;
}
