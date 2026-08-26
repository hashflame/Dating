using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Referrals;

namespace Blizka.UnitTests.UseCases.Referrals;

public sealed class GetReferralStatsQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА запрошена статистика рефералов ТОГДА возвращаются счётчики и сумма зорок из репозитория")]
    public async Task Handle_returns_the_repository_provided_stats()
    {
        var referrerId = Guid.NewGuid();
        var repository = new FakeReferralRepository((5, 3), sparksEarned: 6);
        var handler = new GetReferralStatsQueryHandler(repository);

        var result = await handler.Handle(new GetReferralStatsQuery(referrerId), CancellationToken.None);

        Assert.Equal(5, result.Invited);
        Assert.Equal(3, result.Registered);
        Assert.Equal(6, result.SparksEarned);
    }

    private sealed class FakeReferralRepository((int Invited, int Registered) counts, int sparksEarned) : IReferralRepository
    {
        public Task<Referral?> GetByReferredUserIdAsync(Guid referredUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах статистики.");

        public Task AddAsync(Referral referral, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах статистики.");

        public Task<(int Invited, int Registered)> GetCountsAsync(Guid referrerUserId, CancellationToken cancellationToken) =>
            Task.FromResult(counts);

        public Task<int> GetTotalSparksEarnedAsync(Guid referrerUserId, CancellationToken cancellationToken) =>
            Task.FromResult(sparksEarned);
    }
}
