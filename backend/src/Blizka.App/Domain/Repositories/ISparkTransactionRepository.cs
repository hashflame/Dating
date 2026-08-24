using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

public interface ISparkTransactionRepository
{
    Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken);

    /// <summary>Страница истории операций пользователя, отсортированная по <c>CreatedAt</c> убыв. (T-8.1).</summary>
    Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
