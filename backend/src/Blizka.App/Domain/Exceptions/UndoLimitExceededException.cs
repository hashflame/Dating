namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается при попытке отменить свайп сверх дневного лимита отмен (T-5.3).</summary>
public sealed class UndoLimitExceededException(Guid userId, int limit)
    : BlizkaDomainException(
        "UNDO_LIMIT_EXCEEDED",
        $"User {userId} already used all {limit} undos for the last 24 hours.",
        new Dictionary<string, object?> { ["userId"] = userId, ["limit"] = limit })
{
    public Guid UserId { get; } = userId;

    public int Limit { get; } = limit;
}
