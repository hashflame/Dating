using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

public sealed class TelegramPayment
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public string TelegramPaymentChargeId { get; set; } = string.Empty;

    public int SparkAmount { get; set; }

    public int StarsAmount { get; set; }

    public TelegramPaymentStatus Status { get; set; } = TelegramPaymentStatus.Completed;

    public DateTimeOffset CreatedAt { get; set; }
}
