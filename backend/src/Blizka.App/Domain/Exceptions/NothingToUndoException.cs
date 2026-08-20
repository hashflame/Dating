namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается, когда у пользователя нет активного (не отменённого) свайпа для отмены (T-5.3).</summary>
public sealed class NothingToUndoException(Guid userId)
    : BlizkaDomainException(
        "NOTHING_TO_UNDO",
        $"User {userId} has no active swipe to undo.",
        new Dictionary<string, object?> { ["userId"] = userId })
{
    public Guid UserId { get; } = userId;
}
