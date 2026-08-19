namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается, когда <c>POST /api/onboarding/complete</c> (T-2.3) вызывают для пользователя, который уже не в статусе New.</summary>
public sealed class OnboardingAlreadyCompletedException(Guid userId)
    : BlizkaDomainException(
        "ONBOARDING_ALREADY_COMPLETED",
        $"Onboarding for user {userId} is already completed.",
        new Dictionary<string, object?> { ["userId"] = userId })
{
    public Guid UserId { get; } = userId;
}
