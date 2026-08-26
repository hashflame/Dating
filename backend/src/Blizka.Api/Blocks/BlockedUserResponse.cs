using Blizka.App.UseCases.Blocks;

namespace Blizka.Api.Blocks;

public sealed record BlockedUserResponse(Guid UserId, string Name, string? MainPhotoUrl, DateTimeOffset BlockedAt)
{
    public static BlockedUserResponse From(BlockedUserResult result) =>
        new(result.UserId, result.Name, result.MainPhotoUrl, result.BlockedAt);
}
