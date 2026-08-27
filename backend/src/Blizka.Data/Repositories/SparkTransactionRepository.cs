using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class SparkTransactionRepository(BlizkaDbContext dbContext) : ISparkTransactionRepository
{
    public async Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken) =>
        await dbContext.SparkTransactions.AddAsync(transaction, cancellationToken);

    public Task<bool> ExistsSinceAsync(Guid userId, SparkTransactionType type, DateTimeOffset since, CancellationToken cancellationToken) =>
        dbContext.SparkTransactions
            .AnyAsync(t => t.UserId == userId && t.Type == type && t.CreatedAt >= since, cancellationToken);

    public async Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        // ThenByDescending(Id) — тай-брейкер: несколько операций одного запроса (например, три бонуса за
        // пороги ProfileCompleteness) могут получить одинаковый CreatedAt, а без вторичного ключа сортировки
        // порядок/паginация между страницами были бы недетерминированы.
        var query = dbContext.SparkTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
