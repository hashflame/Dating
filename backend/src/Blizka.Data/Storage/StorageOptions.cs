namespace Blizka.Data.Storage;

/// <summary>Настройки S3-совместимого хранилища фото (T-3.1) — секция <c>Storage</c> в appsettings.yaml.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string Provider { get; set; } = "S3";

    /// <summary>URL S3-совместимого API. Локально — MinIO из docker-compose.yml; в проде — реальный S3-эндпоинт.</summary>
    public string Endpoint { get; set; } = string.Empty;

    public string Bucket { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Базовый URL, из которого собираются публичные ссылки на фото (обычно = Endpoint + Bucket).</summary>
    public string PublicBaseUrl { get; set; } = string.Empty;
}
