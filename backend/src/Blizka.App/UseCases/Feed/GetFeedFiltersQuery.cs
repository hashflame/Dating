using MediatR;

namespace Blizka.App.UseCases.Feed;

/// <summary><c>GET /api/feed/filters</c> (T-5.4) — текущие сохранённые фильтры ленты.</summary>
public sealed record GetFeedFiltersQuery(Guid UserId) : IRequest<FeedFiltersResult>;
