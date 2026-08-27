using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Ideas;

/// <summary>Обрабатывает <see cref="VoteOnIdeaCommand"/> (T-19.1) — идемпотентно, повторный голос не задваивается.</summary>
public sealed class VoteOnIdeaCommandHandler(IIdeaRepository ideaRepository) : IRequestHandler<VoteOnIdeaCommand>
{
    public async Task Handle(VoteOnIdeaCommand request, CancellationToken cancellationToken)
    {
        if (!await ideaRepository.ExistsAsync(request.IdeaId, cancellationToken))
        {
            throw new IdeaNotFoundException(request.IdeaId);
        }

        await ideaRepository.AddVoteAsync(request.IdeaId, request.UserId, cancellationToken);
    }
}
