using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;

namespace Blizka.Data.Repositories;

public sealed class SparkTransactionRepository(BlizkaDbContext dbContext) : ISparkTransactionRepository
{
    public async Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken) =>
        await dbContext.SparkTransactions.AddAsync(transaction, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
