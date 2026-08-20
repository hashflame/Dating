using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;

namespace Blizka.App.Sparks;

public sealed class SparksService(ISparkTransactionRepository sparkTransactionRepository) : ISparksService
{
    public async Task SpendAsync(
        User user, int amount, SparkTransactionType type, Guid? referenceId, CancellationToken cancellationToken)
    {
        if (user.SparksBalance < amount)
        {
            throw new InsufficientSparksException(amount, user.SparksBalance);
        }

        user.SparksBalance -= amount;

        await sparkTransactionRepository.AddAsync(
            new SparkTransaction
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Amount = -amount,
                Type = type,
                ReferenceId = referenceId,
                BalanceAfter = user.SparksBalance,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            cancellationToken);
    }

    public async Task RefundAsync(User user, int amount, Guid referenceId, CancellationToken cancellationToken)
    {
        user.SparksBalance += amount;

        await sparkTransactionRepository.AddAsync(
            new SparkTransaction
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Amount = amount,
                Type = SparkTransactionType.Refund,
                ReferenceId = referenceId,
                BalanceAfter = user.SparksBalance,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            cancellationToken);
    }
}
