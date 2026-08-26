using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Notifications;

public sealed class GetUnreadNotificationsCountQueryHandler(ILikesRepository likesRepository, IMatchRepository matchRepository)
    : IRequestHandler<GetUnreadNotificationsCountQuery, UnreadNotificationsCountResult>
{
    public async Task<UnreadNotificationsCountResult> Handle(GetUnreadNotificationsCountQuery request, CancellationToken cancellationToken)
    {
        var likes = await likesRepository.CountIncomingAsync(request.UserId, cancellationToken);
        var matches = await matchRepository.CountNewAsync(request.UserId, cancellationToken);

        return new UnreadNotificationsCountResult(likes, matches);
    }
}
