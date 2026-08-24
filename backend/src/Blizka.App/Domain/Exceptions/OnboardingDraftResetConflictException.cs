namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда сброс онбординга (<c>DELETE /api/onboarding/draft</c>) столкнулся с параллельным
/// изменением того же пользователя — см. <c>Domain.Repositories.ConcurrentUserUpdateException</c>.
/// Клиенту стоит просто повторить запрос.
/// </summary>
public sealed class OnboardingDraftResetConflictException(Guid userId, Exception innerException)
    : BlizkaDomainException(
        "ONBOARDING_DRAFT_RESET_CONFLICT",
        $"Resetting onboarding for user {userId} conflicted with a concurrent request.",
        new Dictionary<string, object?> { ["userId"] = userId },
        innerException)
{
    public Guid UserId { get; } = userId;
}
