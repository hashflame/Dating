namespace Blizka.Api.Auth;

public sealed record AuthTelegramResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Status,
    bool IsNewUser);
