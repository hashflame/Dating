using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

public sealed class Subscription
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset CurrentPeriodEndAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }
}
