using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary><c>POST /api/matches/{matchId}/archive</c> (T-7.4) — ручная архивация мэтча.</summary>
public sealed record ArchiveMatchCommand(Guid MatchId, Guid UserId) : IRequest;
