using MediatR;

namespace Blizka.App.UseCases.Referrals;

/// <summary><c>POST /api/referrals/invite</c> (T-20.1) — deep link + текст для шаринга приглашения.</summary>
/// <param name="Locale">Локаль запроса ("ru"/"be"/"en") для <see cref="InviteReferralResult.ShareText"/>.</param>
public sealed record InviteReferralCommand(Guid UserId, string Locale) : IRequest<InviteReferralResult>;

public sealed record InviteReferralResult(string Code, string DeepLink, string ShareText);
