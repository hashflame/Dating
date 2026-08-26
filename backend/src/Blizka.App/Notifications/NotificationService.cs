namespace Blizka.App.Notifications;

public sealed class NotificationService(INotificationQueue queue) : INotificationService
{
    public Task NotifyMatchAsync(Guid userId, string matchName, CancellationToken cancellationToken) =>
        queue.EnqueueAsync(new PendingNotification(userId, NotificationType.Match, matchName), cancellationToken).AsTask();

    public Task NotifyNewProfilesAsync(Guid userId, CancellationToken cancellationToken) =>
        queue.EnqueueAsync(new PendingNotification(userId, NotificationType.NewProfiles, Placeholder: null), cancellationToken).AsTask();

    public async Task NotifyCityOpenAsync(IReadOnlyCollection<Guid> userIds, string cityName, CancellationToken cancellationToken)
    {
        foreach (var userId in userIds)
        {
            await queue.EnqueueAsync(new PendingNotification(userId, NotificationType.CityOpen, cityName), cancellationToken);
        }
    }

    public Task NotifyQuestionOfDayBothAnsweredAsync(Guid userId, CancellationToken cancellationToken) =>
        queue.EnqueueAsync(new PendingNotification(userId, NotificationType.QuestionOfDayBothAnswered, Placeholder: null), cancellationToken).AsTask();
}
