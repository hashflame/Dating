namespace Blizka.App.Domain.Enums;

/// <summary>
/// Статус реферальной записи (T-20.1): <c>Pending</c> — приглашённый прошёл Telegram-аутентификацию по
/// ссылке, но ещё не завершил онбординг; <c>Completed</c> — завершил, рефереру начислен бонус.
/// Намеренно не называется <c>Registered</c>/<c>Unregistered</c> — <c>ReferralStatsResult.Registered</c>
/// (T-20.1: <c>GET /api/referrals/stats</c>) означает ровно обратное: "уже завершил онбординг", т.е.
/// соответствует <c>Completed</c>, а не этому значению. Одинаковое слово с противоположным смыслом в
/// enum и в DTO — источник будущих багов, поэтому здесь используется другое имя.
/// </summary>
public enum ReferralStatus
{
    Pending,
    Completed,
}
