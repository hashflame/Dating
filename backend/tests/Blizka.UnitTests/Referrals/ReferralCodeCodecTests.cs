using Blizka.App.Referrals;

namespace Blizka.UnitTests.Referrals;

public sealed class ReferralCodeCodecTests
{
    [Fact(DisplayName = "КОГДА код закодирован и декодирован обратно ТОГДА получается тот же UserId")]
    public void Encode_and_decode_round_trip()
    {
        var userId = Guid.NewGuid();

        var code = ReferralCodeCodec.Encode(userId);
        var decoded = ReferralCodeCodec.TryDecode(code, out var result);

        Assert.True(decoded);
        Assert.Equal(userId, result);
    }

    [Fact(DisplayName = "КОГДА строка не является валидным base64url ТОГДА TryDecode возвращает false")]
    public void TryDecode_returns_false_for_garbage_input()
    {
        Assert.False(ReferralCodeCodec.TryDecode("not-base64!!!", out _));
    }

    [Fact(DisplayName = "КОГДА start_param имеет формат ref_{code} ТОГДА TryDecodeStartParam извлекает UserId")]
    public void TryDecodeStartParam_extracts_the_user_id()
    {
        var userId = Guid.NewGuid();
        var startParam = ReferralCodeCodec.StartParamPrefix + ReferralCodeCodec.Encode(userId);

        var decoded = ReferralCodeCodec.TryDecodeStartParam(startParam, out var result);

        Assert.True(decoded);
        Assert.Equal(userId, result);
    }

    [Theory(DisplayName = "КОГДА start_param пустой, null или без префикса ref_ ТОГДА TryDecodeStartParam возвращает false")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-referral-param")]
    public void TryDecodeStartParam_returns_false_without_the_prefix(string? startParam)
    {
        Assert.False(ReferralCodeCodec.TryDecodeStartParam(startParam, out _));
    }
}
