using Blizka.App.Referrals;
using MediatR;
using Microsoft.Extensions.Options;

namespace Blizka.App.UseCases.Referrals;

/// <summary>
/// Генерирует реферальную ссылку (T-20.1) — код детерминированно кодирует <c>UserId</c>
/// (<see cref="ReferralCodeCodec"/>), поэтому не требует ни отдельного хранения, ни похода в БД:
/// повторный вызов для того же пользователя всегда возвращает тот же код/ссылку.
/// </summary>
public sealed class InviteReferralCommandHandler(IOptions<ReferralOptions> referralOptions)
    : IRequestHandler<InviteReferralCommand, InviteReferralResult>
{
    public Task<InviteReferralResult> Handle(InviteReferralCommand request, CancellationToken cancellationToken)
    {
        var code = ReferralCodeCodec.Encode(request.UserId);
        var deepLink = $"https://t.me/{referralOptions.Value.BotUsername}?start={ReferralCodeCodec.StartParamPrefix}{code}";
        var shareText = ReferralShareTextCatalog.Resolve(deepLink, request.Locale);

        return Task.FromResult(new InviteReferralResult(code, deepLink, shareText));
    }
}
