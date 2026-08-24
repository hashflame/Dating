using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary>
/// Обрабатывает <see cref="UnarchiveMatchCommand"/> (T-7.4) — «бесплатно, всегда» из текста задачи: без списания
/// зорок и без лимита вызовов, в отличие от undo свайпа (T-5.3, 3/сутки). Идемпотентна на уже активном мэтче.
/// Если мэтч всё ещё подпадает под условие протухания (<see cref="MatchArchivalPolicy.IsStale"/>) — восстановление
/// не защищает от повторной автоархивации на следующем прогоне джобы <c>ArchiveStaleMatches</c> (до 6 часов):
/// согласовано с пользователем при уточнении задачи, специальной отсрочки не вводится.
/// </summary>
public sealed class UnarchiveMatchCommandHandler(IMatchRepository matchRepository) : IRequestHandler<UnarchiveMatchCommand>
{
    public async Task Handle(UnarchiveMatchCommand request, CancellationToken cancellationToken)
    {
        var match = await matchRepository.GetByIdForUserTrackedAsync(request.MatchId, request.UserId, cancellationToken)
            ?? throw new MatchNotFoundException(request.MatchId);

        if (match.Status == MatchStatus.Archived)
        {
            match.Status = MatchStatus.Active;
            match.ArchivedAt = null;
            match.ArchivedReason = null;
            await matchRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
