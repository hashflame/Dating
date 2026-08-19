namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается, когда soft-deleted пользователь пытается аутентифицироваться или совершить действие.</summary>
public sealed class UserDeletedException(Guid userId)
    : BlizkaDomainException(
        "USER_DELETED",
        $"User {userId} has been deleted.",
        new Dictionary<string, object?> { ["userId"] = userId })
{
    public Guid UserId { get; } = userId;
}
