using Blizka.App.Domain.Entities;

namespace Blizka.App.Auth;

public interface IJwtTokenService
{
    /// <summary>Issues a session JWT for <paramref name="user"/> with claims userId/telegramId/locale/status (T-1.1).</summary>
    JwtIssuedToken IssueToken(User user);
}

public sealed record JwtIssuedToken(string Token, DateTimeOffset ExpiresAt);
