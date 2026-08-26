namespace Blizka.App.Domain.Repositories;

/// <summary>
/// Выбрасывается репозиторием, когда сохранение нового <c>PrivacySettings</c> конфликтует с уникальным
/// индексом по <c>UserId</c> — т.е. строка для этого пользователя уже была создана параллельным запросом
/// между <c>GetByUserIdTrackedAsync</c> и <c>SaveChangesAsync</c> (двойной PATCH при плохой сети). Предназначено
/// для внутреннего перезапроса в вызывающем коде (по образцу <see cref="ConcurrentUserFilterCreationException"/>),
/// а не для показа клиенту.
/// </summary>
public sealed class ConcurrentPrivacySettingsCreationException(Guid userId, Exception innerException)
    : Exception($"PrivacySettings for user {userId} was created concurrently.", innerException)
{
    public Guid UserId { get; } = userId;
}
