using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Ideas;

/// <summary>Обрабатывает <see cref="RemoveIdeaVoteCommand"/> (T-19.1) — идемпотентно, не проверяет существование идеи (см. doc-комментарий команды).</summary>
public sealed class RemoveIdeaVoteCommandHandler(IIdeaRepository ideaRepository) : IRequestHandler<RemoveIdeaVoteCommand>
{
    public async Task Handle(RemoveIdeaVoteCommand request, CancellationToken cancellationToken) =>
        await ideaRepository.RemoveVoteAsync(request.IdeaId, request.UserId, cancellationToken);
}
