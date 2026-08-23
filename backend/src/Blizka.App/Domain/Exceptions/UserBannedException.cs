namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда забаненный пользователь пытается совершить действие, требующее активного
/// аккаунта. <see cref="Details"/> — ровно <c>{ reason, expiresAt }</c> (spec 002, B2): до T-17.2
/// оба поля проставляются модератором вручную прямой записью в БД, поэтому у уже забаненных без
/// этой правки пользователей они остаются <c>null</c> (бессрочный бан без причины), а не ошибка.
/// </summary>
public sealed class UserBannedException(Guid userId, string? reason, DateTimeOffset? expiresAt)
    : BlizkaDomainException(
        "USER_BANNED",
        $"User {userId} is banned.",
        new Dictionary<string, object?> { ["reason"] = reason, ["expiresAt"] = expiresAt })
{
    public Guid UserId { get; } = userId;
}
