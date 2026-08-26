using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Blocks;

public sealed class GetBlockedUsersQueryHandler(IUserBlockRepository userBlockRepository)
    : IRequestHandler<GetBlockedUsersQuery, IReadOnlyList<BlockedUserResult>>
{
    public async Task<IReadOnlyList<BlockedUserResult>> Handle(GetBlockedUsersQuery request, CancellationToken cancellationToken)
    {
        var blocks = await userBlockRepository.GetBlockedByUserAsync(request.UserId, cancellationToken);

        return blocks
            .Where(b => b.BlockedUser is not null)
            .Select(b => new BlockedUserResult(
                b.BlockedUserId,
                b.BlockedUser!.Name,
                b.BlockedUser.Photos.SingleOrDefault(p => p.IsMain)?.Url,
                b.CreatedAt))
            .ToList();
    }
}
