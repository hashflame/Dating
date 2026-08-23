namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается при попытке свайпнуть сверх дневного лимита (spec 002, B3).</summary>
public sealed class DailySwipeLimitExceededException(Guid userId, DateTimeOffset resetAt)
    : BlizkaDomainException(
        "DAILY_SWIPE_LIMIT_EXCEEDED",
        $"User {userId} already used the daily swipe limit; resets at {resetAt:O}.",
        new Dictionary<string, object?> { ["resetAt"] = resetAt })
{
    public Guid UserId { get; } = userId;

    public DateTimeOffset ResetAt { get; } = resetAt;
}
