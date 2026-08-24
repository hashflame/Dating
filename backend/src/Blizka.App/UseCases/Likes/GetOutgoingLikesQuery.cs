using MediatR;

namespace Blizka.App.UseCases.Likes;

/// <summary><c>GET /api/likes/outgoing</c> (T-6.1).</summary>
public sealed record GetOutgoingLikesQuery(Guid UserId) : IRequest<OutgoingLikesResult>;
