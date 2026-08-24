using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Likes;

/// <summary>Обрабатывает <see cref="GetOutgoingLikesQuery"/> (T-6.1) — кого лайкнул текущий пользователь, без мэтча.</summary>
public sealed class GetOutgoingLikesQueryHandler(ILikesRepository likesRepository)
    : IRequestHandler<GetOutgoingLikesQuery, OutgoingLikesResult>
{
    public async Task<OutgoingLikesResult> Handle(GetOutgoingLikesQuery request, CancellationToken cancellationToken)
    {
        var entries = await likesRepository.GetOutgoingAsync(request.UserId, cancellationToken);
        return new OutgoingLikesResult(entries.Count, entries.Select(LikeResultMapper.ToUserResult).ToList());
    }
}
