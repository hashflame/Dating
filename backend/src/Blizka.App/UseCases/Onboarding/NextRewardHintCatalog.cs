namespace Blizka.App.UseCases.Onboarding;

/// <summary>
/// Поясняющий текст для ближайшего недостигнутого порога <c>ProfileCompleteness</c> (spec 002, B9) —
/// по образцу <c>ErrorMessageCatalog</c> (Blizka.Api), но живёт в Blizka.App и принимает локаль как
/// обычную строку (<c>"ru"/"be"/"en"</c>, как <see cref="Domain.Entities.User.Locale"/>), а не
/// <c>ApiLocale</c> — этот тип определён в Blizka.Api, а App-слой не может на него ссылаться.
/// </summary>
internal static class NextRewardHintCatalog
{
    private static readonly IReadOnlyDictionary<int, (string Ru, string Be, string En)> Hints =
        new Dictionary<int, (string Ru, string Be, string En)>
        {
            [60] = (
                "Добавьте ещё фото или заполните промпты, чтобы получить бонус.",
                "Дадайце яшчэ фота або запоўніце промпты, каб атрымаць бонус.",
                "Add more photos or fill in your prompts to earn a bonus."),
            [80] = (
                "Укажите предпочтения на свидания или голосовое приветствие — и получите бонус.",
                "Пазначце перавагі на спатканні альбо галасавое прывітанне — і атрымаеце бонус.",
                "Add your date preferences or a voice intro to earn a bonus."),
            [100] = (
                "Осталось совсем немного — заполните профиль полностью для последнего бонуса.",
                "Засталося зусім няшмат — запоўніце профіль цалкам дзеля апошняга бонуса.",
                "Just a little more — complete your profile fully for the final bonus."),
        };

    public static string Resolve(int threshold, string locale) => (Hints.TryGetValue(threshold, out var hint), locale) switch
    {
        (true, "be") => hint.Be,
        (true, "en") => hint.En,
        (true, _) => hint.Ru,
        _ => string.Empty,
    };
}
