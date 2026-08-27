namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается при открытии контакта (T-7.3), когда у второго участника мэтча нет публичного username в
/// Telegram — открывать (и списывать зорки за) нечего: без username <c>deepLink</c> некуда вести. Зорки не
/// списываются и <c>Match.ContactUnlockedAt</c> не проставляется, чтобы пользователь мог повторить попытку
/// позже, если второй участник заведёт себе username.
/// </summary>
public sealed class ContactUnlockUnavailableException(Guid matchId)
    : BlizkaDomainException(
        "CONTACT_UNLOCK_UNAVAILABLE",
        $"Cannot unlock contact for match {matchId}: the other participant has no Telegram username.",
        new Dictionary<string, object?> { ["matchId"] = matchId })
{
    public Guid MatchId { get; } = matchId;
}
