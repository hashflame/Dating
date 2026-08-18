namespace Blizka.App.Telegram;

/// <summary>Parsed and HMAC-verified payload of a Telegram WebApp <c>initData</c> string.</summary>
public sealed record TelegramInitData(
    long TelegramId,
    string FirstName,
    string? LastName,
    string? Username,
    string? PhotoUrl,
    string? LanguageCode,
    DateTimeOffset AuthDate);
