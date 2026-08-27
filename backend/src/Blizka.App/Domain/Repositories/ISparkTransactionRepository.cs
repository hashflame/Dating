using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Repositories;

public interface ISparkTransactionRepository
{
    Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken);

    /// <summary>Страница истории операций пользователя, отсортированная по <c>CreatedAt</c> убыв. (T-8.1).</summary>
    Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
        Guid userId, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    /// Была ли уже операция типа <paramref name="type"/> у пользователя, начиная с <paramref name="since"/> —
    /// для помесячного лимита бонуса за отправку идеи (T-19.1: «+✦1 раз в месяц»). Реализация по умолчанию
    /// (для тестовых фейков, которые её не переопределяют, по тому же образцу, что и
    /// <see cref="ILikesRepository.CountIncomingSinceAsync"/>) всегда возвращает <c>false</c> — только настоящая
    /// EF-реализация в <c>Blizka.Data</c> проверяет это по-настоящему.
    /// </summary>
    Task<bool> ExistsSinceAsync(Guid userId, SparkTransactionType type, DateTimeOffset since, CancellationToken cancellationToken) =>
        Task.FromResult(false);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
