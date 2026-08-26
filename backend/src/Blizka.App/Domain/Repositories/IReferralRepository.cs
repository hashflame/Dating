using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

public interface IReferralRepository
{
    /// <summary>Ищет реферальную запись по приглашённому — используется при завершении его онбординга, чтобы начислить бонус рефереру (T-2.3/T-20.1).</summary>
    Task<Referral?> GetByReferredUserIdAsync(Guid referredUserId, CancellationToken cancellationToken);

    Task AddAsync(Referral referral, CancellationToken cancellationToken);

    /// <summary>Счётчики для <c>GET /api/referrals/stats</c>: всего приглашённых по ссылке рефереру и сколько из них завершили онбординг.</summary>
    Task<(int Invited, int Registered)> GetCountsAsync(Guid referrerUserId, CancellationToken cancellationToken);

    /// <summary>Сумма зорок, начисленных рефереру через <c>SparkTransactionType.Referral</c> (T-8.1) — считается по факту начислений, а не Count × текущий ReferralBonusAmount, чтобы не искажаться при изменении конфига.</summary>
    Task<int> GetTotalSparksEarnedAsync(Guid referrerUserId, CancellationToken cancellationToken);
}
