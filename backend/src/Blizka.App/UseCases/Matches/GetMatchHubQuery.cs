using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary><c>GET /api/matches/{matchId}</c> (T-7.2).</summary>
public sealed record GetMatchHubQuery(Guid MatchId, Guid UserId) : IRequest<MatchHubResult>;
