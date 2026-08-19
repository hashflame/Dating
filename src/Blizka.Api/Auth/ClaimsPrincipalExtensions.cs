using System.Security.Claims;

namespace Blizka.Api.Auth;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Читает id пользователя из claim'а <c>userId</c>, который кладёт <see cref="Blizka.App.Auth.JwtTokenService"/> при выдаче токена.</summary>
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst("userId")?.Value;
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new InvalidOperationException("The authenticated principal has no valid 'userId' claim.");
    }
}
