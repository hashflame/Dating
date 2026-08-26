using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

public interface IPrivacySettingsRepository
{
    /// <summary>Без отслеживания изменений — для GET и для батч-подстановки в ленту/хаб мэтча.</summary>
    Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>С отслеживанием изменений — для PATCH (изменения сохраняются через <see cref="SaveChangesAsync"/> без явного Update).</summary>
    Task<PrivacySettings?> GetByUserIdTrackedAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Батч-версия <see cref="GetByUserIdAsync"/> для ленты (T-5.1/T-5.4) — один запрос на весь пул кандидатов
    /// вместо N+1. Пользователи без строки в результат не попадают (вызывающий код трактует отсутствие как дефолты).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, PrivacySettings>> GetByUserIdsAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);

    Task AddAsync(PrivacySettings settings, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
