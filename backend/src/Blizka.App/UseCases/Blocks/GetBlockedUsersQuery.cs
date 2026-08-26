using MediatR;

namespace Blizka.App.UseCases.Blocks;

/// <summary>Список заблокированных текущим пользователем (T-16.2).</summary>
public sealed record GetBlockedUsersQuery(Guid UserId) : IRequest<IReadOnlyList<BlockedUserResult>>;

public sealed record BlockedUserResult(Guid UserId, string Name, string? MainPhotoUrl, DateTimeOffset BlockedAt);
