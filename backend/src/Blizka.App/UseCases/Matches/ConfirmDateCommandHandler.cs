using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary>
/// Обрабатывает <see cref="ConfirmDateCommand"/> (T-12.1) — доступна любому участнику мэтча. Идемпотентна: на
/// уже подтверждённой встрече не сдвигает <c>DateConfirmedAt</c> и не пишет в БД — по тому же принципу, что и
/// <see cref="ArchiveMatchCommandHandler"/>. Фоновая джоба <c>PostDateSurvey</c> (опрос через 24 часа после
/// подтверждения, decomposition.md T-12.1) не реализована — вне рамок MVP-заглушки, согласовано с пользователем.
/// </summary>
public sealed class ConfirmDateCommandHandler(IMatchRepository matchRepository) : IRequestHandler<ConfirmDateCommand>
{
    public async Task Handle(ConfirmDateCommand request, CancellationToken cancellationToken)
    {
        var match = await matchRepository.GetByIdForUserTrackedAsync(request.MatchId, request.UserId, cancellationToken)
            ?? throw new MatchNotFoundException(request.MatchId);

        if (match.DateConfirmedAt is null)
        {
            match.DateConfirmedAt = DateTimeOffset.UtcNow;
            match.DateConfirmedByUserId = request.UserId;
            await matchRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
