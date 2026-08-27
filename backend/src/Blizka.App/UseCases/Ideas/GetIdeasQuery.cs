using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Ideas;

/// <summary>
/// <c>GET /api/ideas?tab=hot|new|inWork|mine&amp;page=1</c> (T-19.1, S-60). <paramref name="Tab"/> — сырая
/// строка из query, а не enum: невалидное значение должно стать 400 VALIDATION_ERROR через
/// <see cref="GetIdeasQueryValidator"/>, а не 500 из-за необработанного парсинга в контроллере.
/// </summary>
public sealed record GetIdeasQuery(Guid UserId, string Tab, int Page, int PageSize) : IRequest<IdeasPageResult>;

public sealed record IdeasPageResult(IReadOnlyList<IdeaItemResult> Items, int TotalCount, int Page, int PageSize);

/// <param name="AuthorName"><c>null</c>, если автор отправил идею анонимно (<c>Idea.IsAnonymous</c>) — независимо от <paramref name="IsMine"/>.</param>
/// <param name="IsMine">Идея принадлежит текущему пользователю.</param>
public sealed record IdeaItemResult(
    Guid Id, string Text, IdeaStatus Status, int VotesCount, bool HasVoted, string? AuthorName, bool IsMine, DateTimeOffset CreatedAt);
