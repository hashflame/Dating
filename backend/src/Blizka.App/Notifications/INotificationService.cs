namespace Blizka.App.Notifications;

/// <summary>Постановка Telegram-уведомлений в очередь на отправку, по типам событий (T-10.2).</summary>
public interface INotificationService
{
    /// <summary>«У вас новый мэтч!» — вызывается для обоих участников мэтча.</summary>
    Task NotifyMatchAsync(Guid userId, string matchName, CancellationToken cancellationToken);

    /// <summary>«Появились новые анкеты».</summary>
    Task NotifyNewProfilesAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>«Мы запустились в {город}!» — рассылка всем из waitlist города (T-4.2).</summary>
    Task NotifyCityOpenAsync(IReadOnlyCollection<Guid> userIds, string cityName, CancellationToken cancellationToken);
}
