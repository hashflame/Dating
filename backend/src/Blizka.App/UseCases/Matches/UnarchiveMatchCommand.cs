using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary><c>DELETE /api/matches/{matchId}/archive</c> (T-7.4) — вернуть мэтч из архива, бесплатно и без ограничения по числу вызовов.</summary>
public sealed record UnarchiveMatchCommand(Guid MatchId, Guid UserId) : IRequest;
