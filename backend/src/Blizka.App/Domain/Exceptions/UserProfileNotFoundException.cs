namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда анкета (<c>GET /api/users/{userId}</c>) не найдена — в том числе для удалённого
/// аккаунта (<c>User.Status == Deleted</c>), который не должен быть доступен для просмотра из списков.
/// </summary>
public sealed class UserProfileNotFoundException(Guid userId)
    : BlizkaDomainException(
        "USER_PROFILE_NOT_FOUND",
        $"User {userId} was not found.",
        new Dictionary<string, object?> { ["userId"] = userId })
{
    public Guid UserId { get; } = userId;
}
