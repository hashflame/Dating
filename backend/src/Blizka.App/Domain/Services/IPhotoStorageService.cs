namespace Blizka.App.Domain.Services;

/// <summary>
/// Абстракция над S3-совместимым хранилищем (в проде — любой S3-провайдер, локально — MinIO, см. docker-compose.yml).
/// Реализация — <c>Blizka.Data</c> (по аналогии с репозиториями: интерфейс в App, инфраструктура в Data).
/// </summary>
public interface IPhotoStorageService
{
    /// <summary>Загружает объект под указанным ключом и возвращает публично доступный URL (на основе <c>Storage:PublicBaseUrl</c>).</summary>
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);

    /// <summary>Скачивает сырые байты объекта под указанным ключом (T-6.1: блюр превью входящих лайков на лету).</summary>
    Task<byte[]> DownloadAsync(string key, CancellationToken cancellationToken);

    /// <summary>Удаляет объект; отсутствие объекта под ключом не считается ошибкой (идемпотентно).</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
