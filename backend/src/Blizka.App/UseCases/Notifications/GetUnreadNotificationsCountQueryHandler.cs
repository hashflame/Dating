using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Notifications;

public sealed class GetUnreadNotificationsCountQueryHandler(
    ILikesRepository likesRepository, IMatchRepository matchRepository, IUserRepository userRepository)
    : IRequestHandler<GetUnreadNotificationsCountQuery, UnreadNotificationsCountResult>
{
    public async Task<UnreadNotificationsCountResult> Handle(GetUnreadNotificationsCountQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Authenticated user {request.UserId} not found.");

        var likes = await likesRepository.CountIncomingSinceAsync(request.UserId, user.LastSeenLikesAt, cancellationToken);
        var matches = await matchRepository.CountNewSinceAsync(request.UserId, user.LastSeenMatchesAt, cancellationToken);

        return new UnreadNotificationsCountResult(likes, matches);
    }
}
