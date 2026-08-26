using Blizka.App.Domain.Enums;

namespace Blizka.App.Notifications;

/// <summary>
/// Тексты Telegram-уведомлений по локалям (T-10.2) — по образцу <c>ErrorMessageCatalog</c> (Blizka.Api), но
/// на локали пользователя (<see cref="Blizka.App.UseCases.Cities.CityLocaleResolver"/> над <c>User.Locale</c>),
/// а не запроса: уведомление шлётся фоново, вне HTTP-контекста.
/// </summary>
public static class NotificationMessageCatalog
{
    private static readonly IReadOnlyDictionary<NotificationType, IReadOnlyDictionary<CityLocale, string>> Templates =
        new Dictionary<NotificationType, IReadOnlyDictionary<CityLocale, string>>
        {
            [NotificationType.Match] = new Dictionary<CityLocale, string>
            {
                [CityLocale.Ru] = "У вас новый мэтч с {0}!",
                [CityLocale.Be] = "У вас новы мэтч з {0}!",
                [CityLocale.En] = "You have a new match with {0}!",
            },
            [NotificationType.NewProfiles] = new Dictionary<CityLocale, string>
            {
                [CityLocale.Ru] = "Появились новые анкеты",
                [CityLocale.Be] = "З'явіліся новыя анкеты",
                [CityLocale.En] = "New profiles are available",
            },
            [NotificationType.CityOpen] = new Dictionary<CityLocale, string>
            {
                [CityLocale.Ru] = "Мы запустились в {0}!",
                [CityLocale.Be] = "Мы запусціліся ў {0}!",
                [CityLocale.En] = "We just launched in {0}!",
            },
            [NotificationType.QuestionOfDayBothAnswered] = new Dictionary<CityLocale, string>
            {
                [CityLocale.Ru] = "Вы оба ответили на вопрос дня — посмотрите ответы друг друга!",
                [CityLocale.Be] = "Вы абодва адказалі на пытанне дня — паглядзіце адказы адно аднаго!",
                [CityLocale.En] = "You both answered the question of the day — check out each other's answers!",
            },
            [NotificationType.DataExportReady] = new Dictionary<CityLocale, string>
            {
                [CityLocale.Ru] = "Ваш архив с данными готов: {0}",
                [CityLocale.Be] = "Ваш архіў з дадзенымі гатовы: {0}",
                [CityLocale.En] = "Your data archive is ready: {0}",
            },
        };

    public static string Build(NotificationType type, CityLocale locale, string? placeholder)
    {
        var template = Templates[type][locale];

        return placeholder is null ? template : string.Format(template, placeholder);
    }
}
