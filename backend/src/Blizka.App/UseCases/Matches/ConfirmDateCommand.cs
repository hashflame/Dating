using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary><c>POST /api/matches/{matchId}/date-confirmed</c> (T-12.1, S-39) — фиксирует договорённость о встрече.</summary>
public sealed record ConfirmDateCommand(Guid MatchId, Guid UserId) : IRequest;
