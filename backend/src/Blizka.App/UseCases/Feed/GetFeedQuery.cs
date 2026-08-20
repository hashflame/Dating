using MediatR;

namespace Blizka.App.UseCases.Feed;

/// <summary><c>GET /api/feed</c> (T-5.1) — очередная порция карточек ленты для текущего пользователя.</summary>
public sealed record GetFeedQuery(Guid UserId, int Limit) : IRequest<FeedResult>;
