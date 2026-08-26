namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается при попытке включить <c>invisibleMode</c> (T-16.1) без активной подписки «Безлимит».</summary>
public sealed class InvisibleModeRequiresSubscriptionException(Guid userId)
    : BlizkaDomainException(
        "INVISIBLE_MODE_REQUIRES_SUBSCRIPTION",
        $"User {userId} has no active subscription required for invisible mode.")
{
    public Guid UserId { get; } = userId;
}
