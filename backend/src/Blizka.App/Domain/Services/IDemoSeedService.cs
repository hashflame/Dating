namespace Blizka.App.Domain.Services;

/// <summary>
/// Пересоздаёт 10 демо-пользователей на prod для ручного тестирования фронтенда (спека 003,
/// <c>POST /api/dev/reseed-demo-data</c>). Реализация — <c>Blizka.Data</c> (по аналогии с репозиториями:
/// интерфейс в App, инфраструктура в Data).
/// </summary>
public interface IDemoSeedService
{
    /// <summary>
    /// Сносит текущих 10 демо-пользователей (если есть) вместе с их фото/интересами/свайпами/мэтчами и
    /// создаёт заново с тем же детерминированным набором TelegramId/Id (см. <c>DemoSeedCatalog</c>) — идемпотентно.
    /// </summary>
    Task<IReadOnlyList<DemoSeedResultUser>> ReseedAsync(CancellationToken cancellationToken);
}

/// <summary>Один демо-пользователь в ответе на пересидирование — чтобы фронтендер знал, под кого логиниться.</summary>
public sealed record DemoSeedResultUser(long TelegramId, string Username, string Name, string? MainPhotoUrl);
