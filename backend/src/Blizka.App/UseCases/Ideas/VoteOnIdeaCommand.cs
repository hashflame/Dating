using MediatR;

namespace Blizka.App.UseCases.Ideas;

/// <summary><c>POST /api/ideas/{ideaId}/vote</c> (T-19.1) — идемпотентно, повторный голос ничего не меняет.</summary>
public sealed record VoteOnIdeaCommand(Guid UserId, Guid IdeaId) : IRequest;
