namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда сохранение отметки «просмотрено» (T-10.2, <c>POST /api/notifications/seen</c>)
/// столкнулось с параллельным изменением того же пользователя (например, два открытых таба почти одновременно
/// гасят один и тот же бейдж, или гонка с обновлением <c>LastActiveAt</c> при логине) — см.
/// <c>Domain.Repositories.ConcurrentUserUpdateException</c>. Клиенту стоит просто повторить запрос.
/// </summary>
public sealed class NotificationsSeenConflictException(Guid userId, Exception innerException)
    : BlizkaDomainException(
        "NOTIFICATIONS_SEEN_CONFLICT",
        $"Marking notifications as seen for {userId} conflicted with a concurrent request.",
        new Dictionary<string, object?> { ["userId"] = userId },
        innerException)
{
    public Guid UserId { get; } = userId;
}
