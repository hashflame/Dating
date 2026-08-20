namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается, когда цель свайпа (userId в маршруте) не найдена — устаревшая карточка ленты на клиенте.</summary>
public sealed class SwipeTargetNotFoundException(Guid userId)
    : BlizkaDomainException(
        "SWIPE_TARGET_NOT_FOUND",
        $"User {userId} was not found.",
        new Dictionary<string, object?> { ["userId"] = userId })
{
    public Guid UserId { get; } = userId;
}
