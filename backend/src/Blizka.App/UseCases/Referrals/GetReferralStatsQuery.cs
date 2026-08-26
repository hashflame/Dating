using MediatR;

namespace Blizka.App.UseCases.Referrals;

/// <summary><c>GET /api/referrals/stats</c> (T-20.1).</summary>
public sealed record GetReferralStatsQuery(Guid UserId) : IRequest<ReferralStatsResult>;

/// <param name="Invited">Всего людей, которые перешли по ссылке и зарегистрировались в Telegram.</param>
/// <param name="Registered">Из них — сколько завершили онбординг (за них рефереру начислен бонус).</param>
/// <param name="SparksEarned">Сумма зорок, начисленных рефереру за приглашённых (T-8.1).</param>
public sealed record ReferralStatsResult(int Invited, int Registered, int SparksEarned);
