using System.IdentityModel.Tokens.Jwt;
using Blizka.App.Auth;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.Auth;

public sealed class JwtTokenServiceTests
{
    private static readonly JwtOptions TestOptions = new()
    {
        Secret = "unit-test-signing-key-at-least-32-bytes-long!!",
        Issuer = "blizka-tests",
        Audience = "blizka-tests-clients",
        TtlHours = 24,
    };

    private static readonly User TestUser = new()
    {
        Id = Guid.NewGuid(),
        TelegramId = 987654321,
        Status = UserStatus.Active,
        Locale = "be",
        Name = "Test User",
    };

    [Fact(DisplayName = "КОГДА выпускается токен ТОГДА в claims попадают userId, telegramId, locale и status")]
    public void IssueToken_sets_userId_telegramId_locale_and_status_claims()
    {
        var service = new JwtTokenService(Options.Create(TestOptions));

        var issued = service.IssueToken(TestUser);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.Token);
        Assert.Equal(TestUser.Id.ToString(), token.Claims.Single(c => c.Type == "userId").Value);
        Assert.Equal(TestUser.TelegramId.ToString(), token.Claims.Single(c => c.Type == "telegramId").Value);
        Assert.Equal("be", token.Claims.Single(c => c.Type == "locale").Value);
        Assert.Equal("Active", token.Claims.Single(c => c.Type == "status").Value);
    }

    [Fact(DisplayName = "КОГДА выпускается токен ТОГДА issuer и audience берутся из опций")]
    public void IssueToken_sets_issuer_and_audience_from_options()
    {
        var service = new JwtTokenService(Options.Create(TestOptions));

        var issued = service.IssueToken(TestUser);

        var token = new JwtSecurityTokenHandler().ReadJwtToken(issued.Token);
        Assert.Equal(TestOptions.Issuer, token.Issuer);
        Assert.Equal(TestOptions.Audience, token.Audiences.Single());
    }

    [Fact(DisplayName = "КОГДА выпускается токен ТОГДА срок его действия истекает через TtlHours часов от текущего момента")]
    public void IssueToken_expires_TtlHours_from_now()
    {
        var service = new JwtTokenService(Options.Create(TestOptions));
        var before = DateTimeOffset.UtcNow;

        var issued = service.IssueToken(TestUser);

        var expectedExpiry = before.AddHours(TestOptions.TtlHours);
        Assert.True(Math.Abs((issued.ExpiresAt - expectedExpiry).TotalSeconds) < 5);
    }
}
