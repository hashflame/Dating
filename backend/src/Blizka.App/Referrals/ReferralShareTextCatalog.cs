namespace Blizka.App.Referrals;

/// <summary>Локализованный текст для шаринга реферальной ссылки (T-20.1) — по образцу <c>NextRewardHintCatalog</c> (Blizka.App.UseCases.Onboarding), локаль как обычная строка ("ru"/"be"/"en").</summary>
internal static class ReferralShareTextCatalog
{
    public static string Resolve(string deepLink, string locale) => locale switch
    {
        "be" => $"Далучайся да Blizka — знаёмствы праз Telegram! {deepLink}",
        "en" => $"Join Blizka — dating right inside Telegram! {deepLink}",
        _ => $"Присоединяйся к Blizka — знакомства прямо в Telegram! {deepLink}",
    };
}
