using MediatR;

namespace Blizka.App.UseCases.Ideas;

/// <summary><c>DELETE /api/ideas/{ideaId}/vote</c> (T-19.1) — идемпотентно, как и <see cref="Blizka.App.UseCases.Blocks.UnblockUserCommand"/>: если голоса не было, тоже успех.</summary>
public sealed record RemoveIdeaVoteCommand(Guid UserId, Guid IdeaId) : IRequest;
