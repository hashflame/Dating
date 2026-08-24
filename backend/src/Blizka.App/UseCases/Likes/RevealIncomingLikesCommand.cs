using MediatR;

namespace Blizka.App.UseCases.Likes;

/// <summary><c>POST /api/likes/incoming/reveal</c> (T-6.1).</summary>
public sealed record RevealIncomingLikesCommand(Guid UserId) : IRequest<RevealIncomingLikesResult>;
