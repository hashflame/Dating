using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary>
/// Обрабатывает <see cref="ArchiveMatchCommand"/> (T-7.4) — ручная архивация, доступна любому участнику мэтча
/// независимо от его состояния (new/waitingForMessage), без ограничений по количеству вызовов. Идемпотентна: на
/// уже заархивированном мэтче не сдвигает <c>ArchivedAt</c> и не пишет в БД — по тому же принципу, что и
/// <see cref="MessageSentCheckCommandHandler"/>.
/// </summary>
public sealed class ArchiveMatchCommandHandler(IMatchRepository matchRepository) : IRequestHandler<ArchiveMatchCommand>
{
    public async Task Handle(ArchiveMatchCommand request, CancellationToken cancellationToken)
    {
        var match = await matchRepository.GetByIdForUserTrackedAsync(request.MatchId, request.UserId, cancellationToken)
            ?? throw new MatchNotFoundException(request.MatchId);

        if (match.Status != MatchStatus.Archived)
        {
            match.Status = MatchStatus.Archived;
            match.ArchivedAt = DateTimeOffset.UtcNow;
            match.ArchivedReason = MatchArchivalPolicy.ManualArchivedReason;
            await matchRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
