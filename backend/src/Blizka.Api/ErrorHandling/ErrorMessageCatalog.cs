namespace Blizka.Api.ErrorHandling;

/// <summary>
/// Сопоставляет <c>ApiError.Code</c> с текстом сообщения для пользователя на каждой поддерживаемой локали.
/// Сообщения объясняют, что делать ("пополните баланс"), а не просто что сломалось — см. decomposition.md T-0.3.
/// </summary>
public static class ErrorMessageCatalog
{
    public const string InsufficientSparks = "INSUFFICIENT_SPARKS";
    public const string UserBanned = "USER_BANNED";
    public const string UserDeleted = "USER_DELETED";
    public const string OnboardingIncomplete = "ONBOARDING_INCOMPLETE";
    public const string OnboardingAlreadyCompleted = "ONBOARDING_ALREADY_COMPLETED";
    public const string CityNotOpen = "CITY_NOT_OPEN";
    public const string CityNotFound = "CITY_NOT_FOUND";
    public const string PhotoLimitExceeded = "PHOTO_LIMIT_EXCEEDED";
    public const string PhotoNotFound = "PHOTO_NOT_FOUND";
    public const string PhotoUploadConflict = "PHOTO_UPLOAD_CONFLICT";
    public const string PhotoDownloadFailed = "PHOTO_DOWNLOAD_FAILED";
    public const string AlreadySwiped = "ALREADY_SWIPED";
    public const string SwipeTargetNotFound = "SWIPE_TARGET_NOT_FOUND";
    public const string SwipeConflict = "SWIPE_CONFLICT";
    public const string NothingToUndo = "NOTHING_TO_UNDO";
    public const string UndoLimitExceeded = "UNDO_LIMIT_EXCEEDED";
    public const string DailySwipeLimitExceeded = "DAILY_SWIPE_LIMIT_EXCEEDED";
    public const string LikesRevealConflict = "LIKES_REVEAL_CONFLICT";
    public const string MatchNotFound = "MATCH_NOT_FOUND";
    public const string QuestionOfDayNotAvailable = "QUESTION_OF_DAY_NOT_AVAILABLE";
    public const string QuestionAnswerConflict = "QUESTION_ANSWER_CONFLICT";
    public const string ContactUnlockConflict = "CONTACT_UNLOCK_CONFLICT";
    public const string OnboardingDraftResetConflict = "ONBOARDING_DRAFT_RESET_CONFLICT";
    public const string InterestNotFound = "INTEREST_NOT_FOUND";
    public const string UserProfileNotFound = "USER_PROFILE_NOT_FOUND";
    public const string InterestCreationConflict = "INTEREST_CREATION_CONFLICT";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string TelegramInitDataInvalid = "TELEGRAM_INIT_DATA_INVALID";
    public const string DevAccessDenied = "DEV_ACCESS_DENIED";
    public const string InvisibleModeRequiresSubscription = "INVISIBLE_MODE_REQUIRES_SUBSCRIPTION";
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
            [OnboardingAlreadyCompleted] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Онбординг уже завершён.",
                [ApiLocale.Be] = "Онбордынг ужо завершаны.",
                [ApiLocale.En] = "Onboarding is already complete.",
            },
            [CityNotOpen] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Ваш город ещё не открыт. Встаньте в лист ожидания — мы уведомим о запуске.",
                [ApiLocale.Be] = "Ваш горад яшчэ не адкрыты. Устаньце ў ліст чакання — мы паведамім пра запуск.",
                [ApiLocale.En] = "Your city isn't open yet. Join the waitlist and we'll notify you at launch.",
            },
            [CityNotFound] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Город не найден.",
                [ApiLocale.Be] = "Горад не знойдзены.",
                [ApiLocale.En] = "City not found.",
            },
            [PhotoLimitExceeded] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Достигнут лимит в 6 фото. Удалите одно из текущих, чтобы загрузить новое.",
                [ApiLocale.Be] = "Дасягнуты ліміт у 6 фота. Выдаліце адно з бягучых, каб загрузіць новае.",
                [ApiLocale.En] = "You've reached the 6-photo limit. Delete one of your current photos to upload a new one.",
            },
            [PhotoNotFound] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Фото не найдено.",
                [ApiLocale.Be] = "Фота не знойдзена.",
                [ApiLocale.En] = "Photo not found.",
            },
            [PhotoUploadConflict] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Не удалось сохранить фото из-за одновременной загрузки. Попробуйте ещё раз.",
                [ApiLocale.Be] = "Не ўдалося захаваць фота з-за адначасовай загрузкі. Паспрабуйце яшчэ раз.",
                [ApiLocale.En] = "Couldn't save the photo due to a concurrent upload. Please try again.",
            },
            [PhotoDownloadFailed] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Не удалось скачать фото по этой ссылке. Попробуйте загрузить его вручную.",
                [ApiLocale.Be] = "Не ўдалося спампаваць фота па гэтай спасылцы. Паспрабуйце загрузіць яго ўручную.",
                [ApiLocale.En] = "Couldn't download the photo from that link. Try uploading it manually.",
            },
            [AlreadySwiped] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Вы уже свайпнули этого пользователя.",
                [ApiLocale.Be] = "Вы ўжо свайпнулі гэтага карыстальніка.",
                [ApiLocale.En] = "You've already swiped this user.",
            },
            [SwipeTargetNotFound] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Этот пользователь больше недоступен.",
                [ApiLocale.Be] = "Гэты карыстальнік больш недаступны.",
                [ApiLocale.En] = "This user is no longer available.",
            },
            [SwipeConflict] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Не удалось обработать свайп из-за одновременного запроса. Попробуйте ещё раз.",
                [ApiLocale.Be] = "Не ўдалося апрацаваць свайп з-за адначасовага запыту. Паспрабуйце яшчэ раз.",
                [ApiLocale.En] = "Couldn't process the swipe due to a concurrent request. Please try again.",
            },
            [NothingToUndo] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Отменять нечего — нет активных свайпов.",
                [ApiLocale.Be] = "Адмяняць няма чаго — няма актыўных свайпаў.",
                [ApiLocale.En] = "Nothing to undo — there are no active swipes.",
            },
            [UndoLimitExceeded] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Лимит отмен на сегодня исчерпан. Попробуйте завтра.",
                [ApiLocale.Be] = "Ліміт адмен на сёння вычарпаны. Паспрабуйце заўтра.",
                [ApiLocale.En] = "You've used all your undos for today. Try again tomorrow.",
            },
            [DailySwipeLimitExceeded] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Дневной лимит свайпов исчерпан. Попробуйте позже.",
                [ApiLocale.Be] = "Дзённы ліміт свайпаў вычарпаны. Паспрабуйце пазней.",
                [ApiLocale.En] = "You've reached today's swipe limit. Try again later.",
            },
            [LikesRevealConflict] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Не удалось разблокировать лайки из-за одновременного запроса. Попробуйте ещё раз.",
                [ApiLocale.Be] = "Не ўдалося разблакаваць лайкі з-за адначасовага запыту. Паспрабуйце яшчэ раз.",
                [ApiLocale.En] = "Couldn't unlock likes due to a concurrent request. Please try again.",
            },
            [MatchNotFound] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Мэтч не найден.",
                [ApiLocale.Be] = "Мэтч не знойдзены.",
                [ApiLocale.En] = "Match not found.",
            },
            [QuestionOfDayNotAvailable] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Вопрос дня ещё не опубликован. Загляните позже.",
                [ApiLocale.Be] = "Пытанне дня яшчэ не апублікавана. Зазірніце пазней.",
                [ApiLocale.En] = "Today's question hasn't been published yet. Check back later.",
            },
            [QuestionAnswerConflict] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Не удалось сохранить ответ из-за одновременного запроса. Попробуйте ещё раз.",
                [ApiLocale.Be] = "Не ўдалося захаваць адказ з-за адначасовага запыту. Паспрабуйце яшчэ раз.",
                [ApiLocale.En] = "Couldn't save your answer due to a concurrent request. Please try again.",
            },
            [ContactUnlockConflict] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Не удалось открыть контакт из-за одновременного запроса. Попробуйте ещё раз.",
                [ApiLocale.Be] = "Не ўдалося адкрыць кантакт з-за адначасовага запыту. Паспрабуйце яшчэ раз.",
                [ApiLocale.En] = "Couldn't unlock the contact due to a concurrent request. Please try again.",
            },
            [OnboardingDraftResetConflict] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Не удалось сбросить онбординг из-за одновременного запроса. Попробуйте ещё раз.",
                [ApiLocale.Be] = "Не ўдалося скінуць онбордынг з-за адначасовага запыту. Паспрабуйце яшчэ раз.",
                [ApiLocale.En] = "Couldn't reset onboarding due to a concurrent request. Please try again.",
            },
            [InterestNotFound] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Интерес не найден в каталоге.",
                [ApiLocale.Be] = "Цікавасць не знойдзена ў каталогу.",
                [ApiLocale.En] = "Interest not found in the catalog.",
            },
            [UserProfileNotFound] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Анкета не найдена.",
                [ApiLocale.Be] = "Анкета не знойдзена.",
                [ApiLocale.En] = "Profile not found.",
            },
            [InterestCreationConflict] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Не удалось сохранить интересы из-за одновременного запроса. Попробуйте ещё раз.",
                [ApiLocale.Be] = "Не ўдалося захаваць цікавасці з-за адначасовага запыту. Паспрабуйце яшчэ раз.",
                [ApiLocale.En] = "Couldn't save your interests due to a concurrent request. Please try again.",
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
            [DevAccessDenied] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Доступ к dev-инструментам закрыт или передан неверный секрет.",
                [ApiLocale.Be] = "Доступ да dev-інструментаў закрыты альбо перададзены няверны сакрэт.",
                [ApiLocale.En] = "Dev tooling access is disabled or the secret provided is invalid.",
            },
            [InvisibleModeRequiresSubscription] = new Dictionary<ApiLocale, string>
            {
                [ApiLocale.Ru] = "Невидимый режим доступен только с подпиской «Безлимит».",
                [ApiLocale.Be] = "Нябачны рэжым даступны толькі з падпіскай «Безлімт».",
                [ApiLocale.En] = "Invisible mode is only available with an Unlimited subscription.",
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
