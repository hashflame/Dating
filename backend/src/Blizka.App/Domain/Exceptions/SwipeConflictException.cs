namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда сохранение свайпа столкнулось с параллельным изменением баланса зорок того же
/// пользователя (например, два суперлайка почти одновременно) — см. <c>Domain.Repositories.ConcurrentUserUpdateException</c>.
/// Клиенту стоит просто повторить запрос.
/// </summary>
public sealed class SwipeConflictException(Guid fromUserId, Exception innerException)
    : BlizkaDomainException(
        "SWIPE_CONFLICT",
        $"Swipe from {fromUserId} conflicted with a concurrent request.",
        new Dictionary<string, object?> { ["userId"] = fromUserId },
        innerException)
{
    public Guid FromUserId { get; } = fromUserId;
}
