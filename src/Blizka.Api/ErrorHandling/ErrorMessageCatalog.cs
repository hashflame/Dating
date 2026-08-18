namespace Blizka.Api.ErrorHandling;

/// <summary>
/// Maps an <c>ApiError.Code</c> to a user-facing, actionable message in each supported locale.
/// Messages say what to do ("top up your balance"), not just what failed — see decomposition.md T-0.3.
/// </summary>
public static class ErrorMessageCatalog
{
    public const string InsufficientSparks = "INSUFFICIENT_SPARKS";
    public const string UserBanned = "USER_BANNED";
    public const string UserDeleted = "USER_DELETED";
    public const string OnboardingIncomplete = "ONBOARDING_INCOMPLETE";
    public const string CityNotOpen = "CITY_NOT_OPEN";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string TelegramInitDataInvalid = "TELEGRAM_INIT_DATA_INVALID";
    public const string InternalError = "INTERNAL_ERROR";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<ApiLocale, string>> Messages =
        new Dictionary<string, IReadOnlyDictionary<ApiLocale, string>>
        {
            [InsufficientSparks] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Недостаточно зорок. Пополните баланс, чтобы продолжить.",
                [ApiLocale.Be] = "Не хапае зорак. Папоўніце баланс, каб працягнуць.",
                [ApiLocale.En] = "Not enough sparks. Top up your balance to continue.",
            },
            [UserBanned] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Ваш аккаунт заблокирован. Свяжитесь с поддержкой, если считаете это ошибкой.",
                [ApiLocale.Be] = "Ваш акаўнт заблакаваны. Звярніцеся ў падтрымку, калі лічыце гэта памылкай.",
                [ApiLocale.En] = "Your account is banned. Contact support if you believe this is a mistake.",
            },
            [UserDeleted] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Этот аккаунт удалён.",
                [ApiLocale.Be] = "Гэты акаўнт выдалены.",
                [ApiLocale.En] = "This account has been deleted.",
            },
            [OnboardingIncomplete] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Регистрация не завершена. Заполните оставшиеся шаги анкеты.",
                [ApiLocale.Be] = "Рэгістрацыя не завершана. Запоўніце астатнія крокі анкеты.",
                [ApiLocale.En] = "Onboarding isn't complete. Finish the remaining profile steps.",
            },
            [CityNotOpen] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Ваш город ещё не открыт. Встаньте в лист ожидания — мы уведомим о запуске.",
                [ApiLocale.Be] = "Ваш горад яшчэ не адкрыты. Устаньце ў ліст чакання — мы паведамім пра запуск.",
                [ApiLocale.En] = "Your city isn't open yet. Join the waitlist and we'll notify you at launch.",
            },
            [ValidationError] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Проверьте правильность заполнения полей.",
                [ApiLocale.Be] = "Праверце правільнасць запаўнення палёў.",
                [ApiLocale.En] = "Please check the fields you've filled in.",
            },
            [TelegramInitDataInvalid] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Не удалось подтвердить данные Telegram. Перезапустите приложение.",
                [ApiLocale.Be] = "Не ўдалося пацвердзіць дадзеныя Telegram. Перазапусціце дадатак.",
                [ApiLocale.En] = "Couldn't verify Telegram data. Please restart the app.",
            },
            [InternalError] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Что-то пошло не так. Попробуйте ещё раз чуть позже.",
                [ApiLocale.Be] = "Нешта пайшло не так. Паспрабуйце яшчэ раз крыху пазней.",
                [ApiLocale.En] = "Something went wrong. Please try again shortly.",
            },
        };

    public static string Resolve(string errorCode, ApiLocale locale)
    {
        if (Messages.TryGetValue(errorCode, out var byLocale))
        {
            if (byLocale.TryGetValue(locale, out var message))
            {
                return message;
            }

            return byLocale[ApiLocaleParser.Default];
        }

        return Messages[InternalError][locale];
    }
}
