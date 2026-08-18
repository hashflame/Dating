namespace Blizka.App.Domain.Exceptions;

/// <summary>Thrown when a soft-deleted user attempts to authenticate or act.</summary>
public sealed class UserDeletedException(Guid userId)
    : BlizkaDomainException(
        "USER_DELETED",
        $"User {userId} has been deleted.",
        new Dictionary<string, object?> { ["userId"] = userId })
{
    public Guid UserId { get; } = userId;
}
