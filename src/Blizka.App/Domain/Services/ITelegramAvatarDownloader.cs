namespace Blizka.App.Domain.Services;

/// <summary>Скачивает аватар пользователя с Telegram CDN для <c>POST /api/users/me/photos/import-telegram</c> (T-3.1).</summary>
public interface ITelegramAvatarDownloader
{
    Task<TelegramAvatarDownload> DownloadAsync(Uri photoUrl, CancellationToken cancellationToken);
}

/// <param name="Content">
/// Тело ответа, уже полностью считанное в память (seekable, <see cref="Stream.Length"/> доступна) — вызывающий
/// код (<c>ImportTelegramPhotoCommandHandler</c>) передаёт поток дальше без промежуточного копирования, ему
/// нужны оба свойства. Владеет потоком вызывающий код и должен его освободить.
/// </param>
/// <param name="ContentType">Content-Type из ответа Telegram CDN (может быть пустым — тогда формат определяется по содержимому).</param>
public sealed record TelegramAvatarDownload(Stream Content, string? ContentType);
