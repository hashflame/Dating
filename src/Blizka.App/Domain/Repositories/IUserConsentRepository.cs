using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Repositories;

public interface IUserConsentRepository
{
    Task AddAsync(UserConsent consent, CancellationToken cancellationToken);

    /// <summary>
    /// Есть ли у пользователя хотя бы одна запись согласия данного типа — для проверки
    /// "без согласия → 422" при <c>POST /api/onboarding/complete</c> (T-2.3).
    /// </summary>
    Task<bool> HasConsentAsync(Guid userId, ConsentType type, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
