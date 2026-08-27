using Blizka.App.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Ideas;

/// <summary>Обрабатывает <see cref="GetIdeasQuery"/> (T-19.1).</summary>
public sealed class GetIdeasQueryHandler(IIdeaRepository ideaRepository, IValidator<GetIdeasQuery> validator)
    : IRequestHandler<GetIdeasQuery, IdeasPageResult>
{
    public async Task<IdeasPageResult> Handle(GetIdeasQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var tab = IdeaListTabParser.Parse(request.Tab);
        var (entries, totalCount) = await ideaRepository.GetPageAsync(
            tab, request.UserId, request.Page, request.PageSize, cancellationToken);

        var items = entries.Select(entry => new IdeaItemResult(
            entry.Idea.Id,
            entry.Idea.Text,
            entry.Idea.Status,
            entry.Idea.VotesCount,
            entry.HasVoted,
            entry.Idea.IsAnonymous ? null : entry.Idea.AuthorUser?.Name,
            entry.Idea.AuthorUserId == request.UserId,
            entry.Idea.CreatedAt)).ToList();

        return new IdeasPageResult(items, totalCount, request.Page, request.PageSize);
    }
}
