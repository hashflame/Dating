using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.Subscriptions;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Matches;

/// <summary>
/// Обрабатывает <see cref="UnlockContactCommand"/> (T-7.3, spec.md 9.1): списывает <c>Sparks:ContactUnlockCost</c>
/// и открывает <c>telegramUsername</c> второго участника навсегда для этого мэтча. Идемпотентно — статус
/// <c>Match.ContactUnlockedAt</c> симметричен для обеих сторон (T-7.2), поэтому повторный вызов (тем же
/// пользователем или вторым участником мэтча) просто возвращает уже доступный контакт без повторного списания —
/// согласовано с пользователем при уточнении задачи, альтернатива (409 «уже открыт») отклонена.
/// Проверка приватности «Запретить писать мне в Telegram» (S-32) зависит от <c>PrivacySettings</c> — таблицы нет
/// в коде, T-16.1 не реализована, поэтому ветка пропущена (тот же MVP-приём, что и <c>writesFirst</c> в T-7.1/T-7.2,
/// согласовано с пользователем). Бесплатный unlock по подписке «Безлимит» (T-8.3) — та же точка расширения
/// <see cref="ISubscriptionChecker"/>, что и дневной лимит свайпов в <c>SwipeCommandHandler</c>: реализация
/// нигде не регистрируется в DI, пока T-8.3 не сделана, поэтому по умолчанию (<c>subscriptionChecker is null</c>)
/// стоимость списывается всегда.
/// </summary>
public sealed class UnlockContactCommandHandler(
    IMatchRepository matchRepository,
    ISparksService sparksService,
    IOptions<SparksOptions> sparksOptions,
    ISubscriptionChecker? subscriptionChecker = null)
    : IRequestHandler<UnlockContactCommand, UnlockContactResult>
{
    public async Task<UnlockContactResult> Handle(UnlockContactCommand request, CancellationToken cancellationToken)
    {
        var match = await matchRepository.GetByIdForUserTrackedAsync(request.MatchId, request.UserId, cancellationToken)
            ?? throw new MatchNotFoundException(request.MatchId);

        var (me, other) = MatchResultMapper.ResolveUsers(match, request.UserId);

        if (match.ContactUnlockedAt is not null)
        {
            return new UnlockContactResult(other.TelegramUsername, BuildDeepLink(other.TelegramUsername), SparksSpent: 0, me.SparksBalance);
        }

        var hasUnlimitedUnlocks = subscriptionChecker is not null &&
            await subscriptionChecker.HasUnlimitedContactUnlocksAsync(me.Id, cancellationToken);
        var cost = hasUnlimitedUnlocks ? 0 : sparksOptions.Value.ContactUnlockCost;

        if (cost > 0)
        {
            await sparksService.SpendAsync(me, cost, SparkTransactionType.ContactUnlock, match.Id, cancellationToken);
        }

        match.ContactUnlockedAt = DateTimeOffset.UtcNow;
        match.ContactUnlockedByUserId = request.UserId;

        try
        {
            await matchRepository.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrentUserUpdateException ex)
        {
            throw new ContactUnlockConflictException(request.MatchId, ex);
        }

        return new UnlockContactResult(other.TelegramUsername, BuildDeepLink(other.TelegramUsername), cost, me.SparksBalance);
    }

    private static string? BuildDeepLink(string? telegramUsername) =>
        telegramUsername is null ? null : $"https://t.me/{telegramUsername}";
}
