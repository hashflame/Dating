using Blizka.App.Domain.Enums;

namespace Blizka.Api.Auth;

/// <summary>Результат обмена валидного Telegram WebApp initData на сессию.</summary>
/// <param name="Token">JWT bearer-токен, который нужно передавать как <c>Authorization: Bearer {token}</c> в последующих запросах.</param>
/// <param name="ExpiresAt">UTC-время истечения <paramref name="Token"/>.</param>
/// <param name="UserId">Id аутентифицированного (или только что созданного) пользователя.</param>
/// <param name="Status">Текущий статус онбординга/аккаунта пользователя.</param>
/// <param name="IsNewUser">Был ли пользователь создан этим вызовом (первый вход через Telegram).</param>
/// <param name="Locale">Локаль пользователя — то же значение, что и claim <c>locale</c> в выданном <paramref name="Token"/>.</param>
public sealed record AuthTelegramResponse(
    string Token,
    DateTimeOffset ExpiresAt,
    Guid UserId,
    UserStatus Status,
    bool IsNewUser,
    string Locale);
