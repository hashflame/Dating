using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Likes;

/// <summary>Обрабатывает <see cref="GetOutgoingLikesQuery"/> (T-6.1) — кого лайкнул текущий пользователь, без мэтча.</summary>
public sealed class GetOutgoingLikesQueryHandler(ILikesRepository likesRepository, IPrivacySettingsRepository privacySettingsRepository)
    : IRequestHandler<GetOutgoingLikesQuery, OutgoingLikesResult>
{
    public async Task<OutgoingLikesResult> Handle(GetOutgoingLikesQuery request, CancellationToken cancellationToken)
    {
        var entries = await likesRepository.GetOutgoingAsync(request.UserId, cancellationToken);
        var privacyByUserId = await privacySettingsRepository.GetByUserIdsAsync(
            entries.Select(e => e.User.Id).ToHashSet(), cancellationToken);
        return new OutgoingLikesResult(entries.Count, LikeResultMapper.ToUserResults(entries, privacyByUserId));
    }
}
