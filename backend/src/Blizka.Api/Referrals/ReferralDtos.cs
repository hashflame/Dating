using Blizka.App.UseCases.Referrals;

namespace Blizka.Api.Referrals;

/// <summary>Ответ <c>POST /api/referrals/invite</c> (T-20.1).</summary>
/// <param name="Code">Реферальный код текущего пользователя.</param>
/// <param name="DeepLink">Ссылка вида <c>https://t.me/{bot}?start=ref_{code}</c> для приглашения.</param>
/// <param name="ShareText">Локализованный текст для шаринга ссылки.</param>
public sealed record ReferralInviteResponse(string Code, string DeepLink, string ShareText)
{
    public static ReferralInviteResponse From(InviteReferralResult result) => new(result.Code, result.DeepLink, result.ShareText);
}

/// <summary>Ответ <c>GET /api/referrals/stats</c> (T-20.1).</summary>
/// <param name="Invited">Всего людей, зарегистрировавшихся по реферальной ссылке.</param>
/// <param name="Registered">Из них завершивших онбординг (за них начислен бонус).</param>
/// <param name="SparksEarned">Всего зорок, заработанных на рефералах.</param>
public sealed record ReferralStatsResponse(int Invited, int Registered, int SparksEarned)
{
    public static ReferralStatsResponse From(ReferralStatsResult result) => new(result.Invited, result.Registered, result.SparksEarned);
}
