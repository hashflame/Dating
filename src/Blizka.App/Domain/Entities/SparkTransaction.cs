using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

public sealed class SparkTransaction
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    /// <summary>Signed: positive for awards, negative for spends.</summary>
    public int Amount { get; set; }

    public SparkTransactionType Type { get; set; }

    public Guid? ReferenceId { get; set; }

    public int BalanceAfter { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
