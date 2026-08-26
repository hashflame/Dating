using Blizka.App.Referrals;
using Blizka.App.UseCases.Referrals;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.Referrals;

public sealed class InviteReferralCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА запрошена реферальная ссылка ТОГДА deepLink содержит бота из конфига и код, который декодируется обратно в UserId")]
    public async Task Handle_builds_a_deep_link_with_a_decodable_code()
    {
        var userId = Guid.NewGuid();
        var handler = new InviteReferralCommandHandler(Options.Create(new ReferralOptions { BotUsername = "blizka_bot" }));

        var result = await handler.Handle(new InviteReferralCommand(userId, "ru"), CancellationToken.None);

        Assert.Equal($"https://t.me/blizka_bot?start=ref_{result.Code}", result.DeepLink);
        Assert.True(ReferralCodeCodec.TryDecode(result.Code, out var decoded));
        Assert.Equal(userId, decoded);
        Assert.Contains(result.DeepLink, result.ShareText);
    }

    [Fact(DisplayName = "КОГДА вызвано дважды для одного пользователя ТОГДА код и ссылка одинаковы (детерминированная генерация)")]
    public async Task Handle_returns_the_same_code_on_repeated_calls()
    {
        var userId = Guid.NewGuid();
        var handler = new InviteReferralCommandHandler(Options.Create(new ReferralOptions { BotUsername = "blizka_bot" }));

        var first = await handler.Handle(new InviteReferralCommand(userId, "ru"), CancellationToken.None);
        var second = await handler.Handle(new InviteReferralCommand(userId, "ru"), CancellationToken.None);

        Assert.Equal(first.Code, second.Code);
        Assert.Equal(first.DeepLink, second.DeepLink);
    }
}
