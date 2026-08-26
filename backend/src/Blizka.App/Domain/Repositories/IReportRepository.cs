using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

public interface IReportRepository
{
    Task AddAsync(Report report, CancellationToken cancellationToken);

    /// <summary>
    /// Пользователи, на которых за окно <paramref name="since"/> пожаловалось не меньше <paramref name="thresholdCount"/>
    /// РАЗНЫХ пользователей — источник для автоматического shadowban'а (T-17.1, job ShadowbanAutoCheck). Считаются
    /// только ещё не рассмотренные (<see cref="Domain.Enums.ReportStatus.Pending"/>) жалобы — отклонённые модератором
    /// (T-17.2) не должны бесконечно давить на порог. Уникальные репортёры, а не жалобы: иначе один и тот же
    /// пользователь мог бы сам себе организовать чужой shadowban тремя жалобами подряд.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetUsersExceedingReportThresholdAsync(
        DateTimeOffset since, int thresholdCount, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
