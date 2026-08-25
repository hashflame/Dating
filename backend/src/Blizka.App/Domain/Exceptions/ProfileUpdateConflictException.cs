namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда сохранение изменений профиля (T-9.1) столкнулось с параллельным изменением того же
/// пользователя (например, два PATCH /api/users/me/profile почти одновременно) — см.
/// <c>Domain.Repositories.ConcurrentUserUpdateException</c>. Клиенту стоит просто повторить запрос.
/// </summary>
public sealed class ProfileUpdateConflictException(Guid userId, Exception innerException)
    : BlizkaDomainException(
        "PROFILE_UPDATE_CONFLICT",
        $"Updating the profile for {userId} conflicted with a concurrent request.",
        new Dictionary<string, object?> { ["userId"] = userId },
        innerException)
{
    public Guid UserId { get; } = userId;
}
