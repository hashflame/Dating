using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary><c>POST /api/matches/{matchId}/unlock</c> (T-7.3, spec.md 9.1).</summary>
public sealed record UnlockContactCommand(Guid MatchId, Guid UserId) : IRequest<UnlockContactResult>;
