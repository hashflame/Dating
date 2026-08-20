using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Swipes;

/// <summary><c>POST /api/feed/{userId}/like|dislike|superlike</c> (T-5.2).</summary>
public sealed record SwipeCommand(Guid FromUserId, Guid ToUserId, SwipeType Type) : IRequest<SwipeResult>;
