namespace Blizka.App.Notifications;

/// <summary>
/// Очередь Telegram-уведомлений (T-10.2) — «Channel + BackgroundService» вместо Quartz-джобы: уведомления
/// рождаются от конкретных событий (мэтч, открытие города), а не по расписанию, поэтому опрос по таймеру
/// добавил бы только задержку. Писатель — <see cref="INotificationService"/> (App), читатель — фоновый
/// сервис в Blizka.Host, который резолвит пользователя и шлёт сообщение через <c>ITelegramBotService</c>.
/// </summary>
public interface INotificationQueue
{
    ValueTask EnqueueAsync(PendingNotification notification, CancellationToken cancellationToken);

    IAsyncEnumerable<PendingNotification> ReadAllAsync(CancellationToken cancellationToken);
}
