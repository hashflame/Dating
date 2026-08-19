namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается при попытке загрузить фото сверх лимита в 6 штук на пользователя (T-3.1).</summary>
public sealed class PhotoLimitExceededException(Guid userId, int limit)
    : BlizkaDomainException(
        "PHOTO_LIMIT_EXCEEDED",
        $"User {userId} already has the maximum of {limit} photos.",
        new Dictionary<string, object?> { ["userId"] = userId, ["limit"] = limit })
{
    public Guid UserId { get; } = userId;

    public int Limit { get; } = limit;
}
