using Blizka.App.Notifications;

namespace Blizka.UnitTests.Notifications;

public sealed class NotificationServiceTests
{
    [Fact(DisplayName = "КОГДА NotifyMatchAsync ТОГДА в очередь ставится уведомление Match с именем мэтча")]
    public async Task NotifyMatchAsync_enqueues_a_match_notification()
    {
        var queue = new FakeNotificationQueue();
        var service = new NotificationService(queue);
        var userId = Guid.NewGuid();

        await service.NotifyMatchAsync(userId, "Анна", CancellationToken.None);

        var enqueued = Assert.Single(queue.Enqueued);
        Assert.Equal(userId, enqueued.UserId);
        Assert.Equal(NotificationType.Match, enqueued.Type);
        Assert.Equal("Анна", enqueued.Placeholder);
    }

    [Fact(DisplayName = "КОГДА NotifyNewProfilesAsync ТОГДА в очередь ставится уведомление NewProfiles без плейсхолдера")]
    public async Task NotifyNewProfilesAsync_enqueues_a_new_profiles_notification()
    {
        var queue = new FakeNotificationQueue();
        var service = new NotificationService(queue);
        var userId = Guid.NewGuid();

        await service.NotifyNewProfilesAsync(userId, CancellationToken.None);

        var enqueued = Assert.Single(queue.Enqueued);
        Assert.Equal(userId, enqueued.UserId);
        Assert.Equal(NotificationType.NewProfiles, enqueued.Type);
        Assert.Null(enqueued.Placeholder);
    }

    [Fact(DisplayName = "КОГДА NotifyCityOpenAsync с несколькими пользователями ТОГДА в очередь ставится по одному уведомлению на каждого")]
    public async Task NotifyCityOpenAsync_enqueues_one_notification_per_user()
    {
        var queue = new FakeNotificationQueue();
        var service = new NotificationService(queue);
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        await service.NotifyCityOpenAsync([userId1, userId2], "Минск", CancellationToken.None);

        Assert.Equal(2, queue.Enqueued.Count);
        Assert.All(queue.Enqueued, n =>
        {
            Assert.Equal(NotificationType.CityOpen, n.Type);
            Assert.Equal("Минск", n.Placeholder);
        });
        Assert.Contains(queue.Enqueued, n => n.UserId == userId1);
        Assert.Contains(queue.Enqueued, n => n.UserId == userId2);
    }

    private sealed class FakeNotificationQueue : INotificationQueue
    {
        public List<PendingNotification> Enqueued { get; } = [];

        public ValueTask EnqueueAsync(PendingNotification notification, CancellationToken cancellationToken)
        {
            Enqueued.Add(notification);
            return ValueTask.CompletedTask;
        }

        public IAsyncEnumerable<PendingNotification> ReadAllAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах NotificationService.");
    }
}
