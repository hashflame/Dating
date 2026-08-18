namespace Blizka.App.Domain.Exceptions;

/// <summary>Thrown when a banned user attempts an action that requires an active account.</summary>
public sealed class UserBannedException(Guid userId)
    : BlizkaDomainException(
        "USER_BANNED",
        $"User {userId} is banned.",
        new Dictionary<string, object?> { ["userId"] = userId })
{
    public Guid UserId { get; } = userId;
}
