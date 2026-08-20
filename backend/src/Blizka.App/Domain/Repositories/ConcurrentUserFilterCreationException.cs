namespace Blizka.App.Domain.Repositories;

/// <summary>
/// Выбрасывается репозиторием, когда сохранение нового <c>UserFilter</c> конфликтует с первичным
/// ключом по <c>UserId</c> — т.е. фильтр для этого пользователя уже был создан параллельным запросом
/// между <c>GetAsync</c> и <c>SaveChangesAsync</c> (двойной PATCH при плохой сети). Предназначено для
/// внутреннего перезапроса в вызывающем коде (по образцу <see cref="ConcurrentOnboardingDraftCreationException"/>),
/// а не для показа клиенту.
/// </summary>
public sealed class ConcurrentUserFilterCreationException(Guid userId, Exception innerException)
    : Exception($"UserFilter for user {userId} was created concurrently.", innerException)
{
    public Guid UserId { get; } = userId;
}
