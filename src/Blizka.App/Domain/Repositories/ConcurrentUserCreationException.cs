namespace Blizka.App.Domain.Repositories;

/// <summary>
/// Выбрасывается репозиторием, когда сохранение нового <c>User</c> конфликтует с уникальным
/// индексом по <c>TelegramId</c> — т.е. пользователь с этим telegramId уже был создан
/// параллельным запросом между <c>GetByTelegramIdAsync</c> и <c>SaveChangesAsync</c>.
/// Предназначено для внутреннего перезапроса в вызывающем коде, а не для показа клиенту.
/// </summary>
public sealed class ConcurrentUserCreationException(long telegramId, Exception innerException)
    : Exception($"User with telegramId {telegramId} was created concurrently.", innerException)
{
    public long TelegramId { get; } = telegramId;
}
