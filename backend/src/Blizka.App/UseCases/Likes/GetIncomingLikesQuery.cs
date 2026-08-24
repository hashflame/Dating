using MediatR;

namespace Blizka.App.UseCases.Likes;

/// <summary><c>GET /api/likes/incoming</c> (T-6.1).</summary>
public sealed record GetIncomingLikesQuery(Guid UserId) : IRequest<IncomingLikesResult>;
