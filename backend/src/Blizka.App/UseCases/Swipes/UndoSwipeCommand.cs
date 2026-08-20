using MediatR;

namespace Blizka.App.UseCases.Swipes;

/// <summary><c>POST /api/feed/undo</c> (T-5.3).</summary>
public sealed record UndoSwipeCommand(Guid UserId) : IRequest<UndoSwipeResult>;
