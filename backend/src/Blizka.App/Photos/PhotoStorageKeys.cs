namespace Blizka.App.Photos;

/// <summary>Единая схема ключей объектов фото в S3-совместимом хранилище — используется при загрузке и при удалении.</summary>
public static class PhotoStorageKeys
{
    public static string Prefix(Guid userId, Guid photoId) => $"photos/{userId:N}/{photoId:N}";

    public static string Original(string prefix, string extension) => $"{prefix}/original.{extension}";

    public static string Thumbnail(string prefix) => $"{prefix}/thumbnail.jpg";

    public static string Medium(string prefix) => $"{prefix}/medium.jpg";

    /// <summary>Извлекает расширение оригинала из его публичного URL (ключ на диске не хранится отдельно от Photo).</summary>
    public static string ExtensionFromUrl(string url)
    {
        var path = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsolutePath : url;
        var lastDot = path.LastIndexOf('.');
        return lastDot >= 0 && lastDot < path.Length - 1 ? path[(lastDot + 1)..] : "jpg";
    }
}
