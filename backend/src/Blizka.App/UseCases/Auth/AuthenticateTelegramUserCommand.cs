using Blizka.App.Telegram;
using MediatR;

namespace Blizka.App.UseCases.Auth;

public sealed record AuthenticateTelegramUserCommand(TelegramInitData InitData)
    : IRequest<AuthenticateTelegramUserResult>;

public sealed record AuthenticateTelegramUserResult(
    string Token,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    string Status,
    bool IsNewUser,
    string Locale);
