using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary><c>GET /api/matches</c> (T-7.1).</summary>
public sealed record GetMatchesQuery(Guid UserId) : IRequest<MatchesResult>;
