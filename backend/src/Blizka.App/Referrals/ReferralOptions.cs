namespace Blizka.App.Referrals;

/// <summary>Настройки генерации реферальных ссылок (T-20.1) — секция <c>Referral</c> в appsettings.yaml.</summary>
public sealed class ReferralOptions
{
    public const string SectionName = "Referral";

    /// <summary>Username Telegram-бота без <c>@</c>, используется в deep link <c>https://t.me/{BotUsername}?start=ref_{code}</c> (decomposition.md T-20.1).</summary>
    public string BotUsername { get; set; } = "blizka_bot";
}
