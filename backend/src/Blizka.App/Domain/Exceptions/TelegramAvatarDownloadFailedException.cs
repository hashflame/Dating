namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда <c>ITelegramAvatarDownloader</c> не смог скачать аватар по присланному клиентом
/// <c>photoUrl</c> (Telegram CDN вернул не-2xx, соединение оборвалось/протухло или файл превысил лимит
/// размера) — клиентская, а не серверная проблема: URL был корректным по формату, но недоступен на момент
/// запроса. Раньше здесь пробрасывались "сырые" <c>HttpRequestException</c>/<c>InvalidOperationException</c>,
/// которые <c>BlizkaExceptionHandler</c> классифицировал как непредвиденный 500.
/// </summary>
public sealed class TelegramAvatarDownloadFailedException(Uri photoUrl, Exception innerException)
    : BlizkaDomainException(
        "PHOTO_DOWNLOAD_FAILED",
        $"Failed to download Telegram avatar from {photoUrl}.",
        new Dictionary<string, object?> { ["photoUrl"] = photoUrl.ToString() },
        innerException)
{
    public Uri PhotoUrl { get; } = photoUrl;
}
