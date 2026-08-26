using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class ReferralRepository(BlizkaDbContext dbContext) : IReferralRepository
{
    public Task<Referral?> GetByReferredUserIdAsync(Guid referredUserId, CancellationToken cancellationToken) =>
        dbContext.Referrals.SingleOrDefaultAsync(r => r.ReferredUserId == referredUserId, cancellationToken);

    public async Task AddAsync(Referral referral, CancellationToken cancellationToken) =>
        await dbContext.Referrals.AddAsync(referral, cancellationToken);

    public async Task<(int Invited, int Registered)> GetCountsAsync(Guid referrerUserId, CancellationToken cancellationToken)
    {
        var invited = await dbContext.Referrals.AsNoTracking()
            .CountAsync(r => r.ReferrerUserId == referrerUserId, cancellationToken);
        var registered = await dbContext.Referrals.AsNoTracking()
            .CountAsync(r => r.ReferrerUserId == referrerUserId && r.Status == ReferralStatus.Completed, cancellationToken);

        return (invited, registered);
    }

    public async Task<int> GetTotalSparksEarnedAsync(Guid referrerUserId, CancellationToken cancellationToken) =>
        await dbContext.SparkTransactions.AsNoTracking()
            .Where(t => t.UserId == referrerUserId && t.Type == SparkTransactionType.Referral)
            .Select(t => (int?)t.Amount)
            .SumAsync(cancellationToken) ?? 0;
}
