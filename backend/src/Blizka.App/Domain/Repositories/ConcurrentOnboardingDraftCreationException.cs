namespace Blizka.App.Domain.Repositories;

/// <summary>
/// Выбрасывается репозиторием, когда сохранение нового <c>OnboardingDraft</c> конфликтует с первичным
/// ключом по <c>UserId</c> — т.е. черновик для этого пользователя уже был создан параллельным запросом
/// между <c>GetAsync</c> и <c>SaveChangesAsync</c> (например, двойной PATCH при плохой сети).
/// Предназначено для внутреннего перезапроса в вызывающем коде, а не для показа клиенту.
/// </summary>
public sealed class ConcurrentOnboardingDraftCreationException(Guid userId, Exception innerException)
    : Exception($"OnboardingDraft for user {userId} was created concurrently.", innerException)
{
    public Guid UserId { get; } = userId;
}
