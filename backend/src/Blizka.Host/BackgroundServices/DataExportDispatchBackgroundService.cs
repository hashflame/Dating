using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.App.DataExport;
using Blizka.App.Domain.Services;
using Blizka.App.Notifications;
using Blizka.App.UseCases.Users;
using MediatR;

namespace Blizka.Host.BackgroundServices;

/// <summary>
/// Читает <see cref="IDataExportQueue"/> (T-16.2) — по образцу <see cref="NotificationDispatchBackgroundService"/>:
/// собирает JSON-архив данных пользователя (<c>BuildDataExportQuery</c>), заливает в S3-совместимое хранилище
/// и ставит в очередь Telegram-уведомление со ссылкой на скачивание. Сбои единичных запросов логируются и не
/// прерывают цикл — по тем же соображениям, что и в <see cref="NotificationDispatchBackgroundService"/>.
/// </summary>
public sealed class DataExportDispatchBackgroundService(
    IDataExportQueue queue, IServiceScopeFactory scopeFactory, ILogger<DataExportDispatchBackgroundService> logger)
    : BackgroundService
{
    // Архив содержит PII (TelegramId, TelegramUsername, Bio, город и т.п.) — в отличие от фото профиля,
    // которые обязаны быть публичными, ссылка на него не должна жить бессрочно у всех, кто её увидел
    // (пересланное сообщение, скриншот, лог прокси). 24 часа — разумный запас, чтобы пользователь успел
    // скачать архив, не оставляя ссылку рабочей навсегда.
    private static readonly TimeSpan DownloadLinkValidFor = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var request in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await DispatchAsync(request, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Не удалось собрать экспорт данных для пользователя {UserId}", request.UserId);
            }
        }
    }

    private async Task DispatchAsync(PendingDataExportRequest request, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var storageService = scope.ServiceProvider.GetRequiredService<IPhotoStorageService>();
        var notificationQueue = scope.ServiceProvider.GetRequiredService<INotificationQueue>();

        var payload = await mediator.Send(new BuildDataExportQuery(request.UserId), cancellationToken);
        var json = JsonSerializer.Serialize(payload, SerializerOptions);

        var key = DataExportStorageKeys.Archive(request.UserId, Guid.NewGuid());
        using (var content = new MemoryStream(Encoding.UTF8.GetBytes(json)))
        {
            // Возврат UploadAsync (постоянный публичный URL) осознанно игнорируется — см. GetTemporaryDownloadUrlAsync ниже.
            await storageService.UploadAsync(key, content, "application/json", cancellationToken);
        }

        var url = await storageService.GetTemporaryDownloadUrlAsync(key, DownloadLinkValidFor, cancellationToken);

        await notificationQueue.EnqueueAsync(
            new PendingNotification(request.UserId, NotificationType.DataExportReady, url), cancellationToken);

        logger.LogInformation("DataExport: архив для пользователя {UserId} собран и залит по ключу {Key}", request.UserId, key);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
