using System.Threading.Channels;

namespace Blizka.App.Notifications;

/// <summary>Реализация <see cref="INotificationQueue"/> поверх <see cref="Channel{T}"/> — неограниченная, процесс не переживает рестарт (T-10.2, MVP: без персистентной очереди).</summary>
public sealed class NotificationQueue : INotificationQueue
{
    private readonly Channel<PendingNotification> _channel = Channel.CreateUnbounded<PendingNotification>();

    public ValueTask EnqueueAsync(PendingNotification notification, CancellationToken cancellationToken) =>
        _channel.Writer.WriteAsync(notification, cancellationToken);

    public IAsyncEnumerable<PendingNotification> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
