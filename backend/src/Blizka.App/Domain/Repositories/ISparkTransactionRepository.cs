using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

public interface ISparkTransactionRepository
{
    Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
