namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается, когда забаненный пользователь пытается совершить действие, требующее активного аккаунта.</summary>
public sealed class UserBannedException(Guid userId)
    : BlizkaDomainException(
        "USER_BANNED",
        $"User {userId} is banned.",
        new Dictionary<string, object?> { ["userId"] = userId })
{
    public Guid UserId { get; } = userId;
}
