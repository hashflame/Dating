namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается при попытке свайпнуть пользователя, которого текущий пользователь уже свайпнул (и не отменил, T-5.3).</summary>
public sealed class AlreadySwipedException(Guid toUserId, Exception? innerException = null)
    : BlizkaDomainException(
        "ALREADY_SWIPED",
        $"User {toUserId} was already swiped.",
        new Dictionary<string, object?> { ["userId"] = toUserId },
        innerException)
{
    public Guid ToUserId { get; } = toUserId;
}
