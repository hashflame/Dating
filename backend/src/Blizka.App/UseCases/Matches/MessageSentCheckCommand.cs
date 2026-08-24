using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary><c>POST /api/matches/{matchId}/message-sent-check</c> (T-7.3, spec.md 9.3) — фронт вызывает после возврата из Telegram deep link'а.</summary>
public sealed record MessageSentCheckCommand(Guid MatchId, Guid UserId) : IRequest;
