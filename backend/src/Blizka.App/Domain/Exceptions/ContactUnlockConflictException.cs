namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда открытие контакта (T-7.3) столкнулось с параллельным изменением баланса зорок того же
/// пользователя (например, двойное нажатие «Открыть контакт») — см. <c>Domain.Repositories.ConcurrentUserUpdateException</c>.
/// Клиенту стоит просто повторить запрос.
/// </summary>
public sealed class ContactUnlockConflictException(Guid matchId, Exception innerException)
    : BlizkaDomainException(
        "CONTACT_UNLOCK_CONFLICT",
        $"Unlocking contact for match {matchId} conflicted with a concurrent request.",
        new Dictionary<string, object?> { ["matchId"] = matchId },
        innerException)
{
    public Guid MatchId { get; } = matchId;
}
