namespace Blizka.App.Telegram;

/// <summary>Распарсенный и HMAC-верифицированный payload строки Telegram WebApp <c>initData</c>.</summary>
public sealed record TelegramInitData(
    long TelegramId,
    string FirstName,
    string? LastName,
    string? Username,
    string? PhotoUrl,
    string? LanguageCode,
    DateTimeOffset AuthDate);
