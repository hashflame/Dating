namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда сохранение разблокировки входящих лайков (T-6.1) столкнулось с параллельным изменением
/// баланса зорок того же пользователя (например, двойное нажатие «Разблокировать») — см.
/// <c>Domain.Repositories.ConcurrentUserUpdateException</c>. Клиенту стоит просто повторить запрос.
/// </summary>
public sealed class LikesRevealConflictException(Guid userId, Exception innerException)
    : BlizkaDomainException(
        "LIKES_REVEAL_CONFLICT",
        $"Revealing incoming likes for {userId} conflicted with a concurrent request.",
        new Dictionary<string, object?> { ["userId"] = userId },
        innerException)
{
    public Guid UserId { get; } = userId;
}
