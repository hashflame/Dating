using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary>
/// Обрабатывает <see cref="MessageSentCheckCommand"/> (T-7.3, spec.md 9.3) — decomposition.md прямо называет это
/// метрикой/аналитикой, без бизнес-эффекта. Проставляет <c>Match.MessageSentCheckAt</c> идемпотентно, один раз:
/// повторные вызовы (пользователь снова уходит в Telegram и возвращается) не сдвигают момент — иначе окно
/// архивации T-7.4 («нет message-sent-check более 7 дней после открытия контакта») продлевалось бы бесконечно
/// от одного и того же нажатия кнопки (согласовано с пользователем при уточнении задачи).
/// </summary>
public sealed class MessageSentCheckCommandHandler(IMatchRepository matchRepository)
    : IRequestHandler<MessageSentCheckCommand>
{
    public async Task Handle(MessageSentCheckCommand request, CancellationToken cancellationToken)
    {
        var match = await matchRepository.GetByIdForUserTrackedAsync(request.MatchId, request.UserId, cancellationToken)
            ?? throw new MatchNotFoundException(request.MatchId);

        if (match.MessageSentCheckAt is null)
        {
            match.MessageSentCheckAt = DateTimeOffset.UtcNow;
            await matchRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
