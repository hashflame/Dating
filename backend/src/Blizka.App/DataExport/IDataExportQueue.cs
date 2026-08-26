namespace Blizka.App.DataExport;

/// <summary>
/// Очередь запросов на экспорт данных (T-16.2) — «Channel + BackgroundService», по образцу
/// <see cref="Blizka.App.Notifications.INotificationQueue"/> (T-10.2): запрос рождается по требованию
/// пользователя, а не по расписанию, поэтому Quartz-джоба с триггером не нужна. Писатель —
/// <c>RequestDataExportCommandHandler</c> (App), читатель — фоновый сервис в Blizka.Host, который собирает
/// JSON-архив, заливает в S3-совместимое хранилище и шлёт ссылку через Telegram.
/// </summary>
public interface IDataExportQueue
{
    ValueTask EnqueueAsync(PendingDataExportRequest request, CancellationToken cancellationToken);

    IAsyncEnumerable<PendingDataExportRequest> ReadAllAsync(CancellationToken cancellationToken);
}
