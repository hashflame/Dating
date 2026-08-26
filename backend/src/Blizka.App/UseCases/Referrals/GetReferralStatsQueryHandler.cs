using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Referrals;

public sealed class GetReferralStatsQueryHandler(IReferralRepository referralRepository)
    : IRequestHandler<GetReferralStatsQuery, ReferralStatsResult>
{
    public async Task<ReferralStatsResult> Handle(GetReferralStatsQuery request, CancellationToken cancellationToken)
    {
        var (invited, registered) = await referralRepository.GetCountsAsync(request.UserId, cancellationToken);
        var sparksEarned = await referralRepository.GetTotalSparksEarnedAsync(request.UserId, cancellationToken);

        return new ReferralStatsResult(invited, registered, sparksEarned);
    }
}
