using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Services;

namespace Blizka.Data.Http;

/// <summary>
/// Скачивает файл аватара с Telegram CDN для <c>POST /api/users/me/photos/import-telegram</c> (T-3.1).
/// Хост уже ограничен валидатором в App-слое (<c>ImportTelegramPhotoCommandValidator</c>, защита от SSRF) —
/// здесь дополнительно ограничивается объём скачиваемых данных, чтобы недоступный размер ответа (сервер
/// Telegram может не прислать Content-Length) не привёл к неограниченному чтению в память.
/// Любая ошибка скачивания (недоступный/протухший URL, обрыв соединения, превышение лимита размера)
/// оборачивается в <see cref="TelegramAvatarDownloadFailedException"/> — это проблема на стороне
/// присланного клиентом URL, а не сервера, и не должна превращаться в 500.
/// </summary>
public sealed class TelegramAvatarDownloader(HttpClient httpClient) : ITelegramAvatarDownloader
{
    private const long MaxDownloadBytes = 10 * 1024 * 1024;
    private const int BufferSize = 81920;

    public async Task<TelegramAvatarDownload> DownloadAsync(Uri photoUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(photoUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            var contentType = response.Content.Headers.ContentType?.MediaType;

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var buffer = new MemoryStream();
            var readBuffer = new byte[BufferSize];
            long total = 0;
            int bytesRead;
            while ((bytesRead = await responseStream.ReadAsync(readBuffer, cancellationToken)) > 0)
            {
                total += bytesRead;
                if (total > MaxDownloadBytes)
                {
                    throw new TelegramAvatarDownloadFailedException(
                        photoUrl,
                        new InvalidOperationException($"Telegram avatar at {photoUrl} exceeds the {MaxDownloadBytes}-byte limit."));
                }

                await buffer.WriteAsync(readBuffer.AsMemory(0, bytesRead), cancellationToken);
            }

            buffer.Position = 0;
            return new TelegramAvatarDownload(buffer, contentType);
        }
        catch (HttpRequestException ex)
        {
            throw new TelegramAvatarDownloadFailedException(photoUrl, ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout (заданный в DataServiceCollectionExtensions) всплывает как TaskCanceledException,
            // а не TimeoutException — отличаем его от отмены самим вызывающим по состоянию токена отмены.
            throw new TelegramAvatarDownloadFailedException(photoUrl, ex);
        }
    }
}
