using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Matches;

/// <summary>
/// Фиксированный каталог идей свидания (T-12.1) — MVP-заглушка вместо реальной LLM-генерации, которая ждёт
/// T-13.1 (AI-сервис генерации сообщений: тот же паттерн внешнего LLM-клиента понадобится и здесь). Согласовано
/// с пользователем при уточнении задачи: пока T-13.1 не готова, <see cref="GetDateIdeasQueryHandler"/> подбирает
/// идеи из этого каталога по пересечению предпочтений на свидания (T-9.3) обоих участников мэтча.
/// </summary>
internal static class DateIdeaCatalog
{
    public const string DefaultCurrency = "BYN";
    public const string DefaultCityPlaceholder = "вашем городе";

    // Все шаблоны построены вокруг единственной конструкции «в {0}» (нашлось при подстановке реального имени
    // города вместо DefaultCityPlaceholder — часть шаблонов ждала родительный/дательный падеж без предлога,
    // «по центру {0}» → «по центру Минск» вместо «по центру Минска», найдено вручную, тикет ClickUp).
    // {0} подставляется уже в предложном падеже (см. CityLocativeNames), поэтому здесь везде только «в {0}».
    private static readonly IReadOnlyList<Template> Templates =
    [
        new(DatePreferenceCode.ActiveOutdoors, "Прогулка в парке",
            "Неспешная прогулка по одному из парков в {0}, с кофе навынос.", 10m, "1-2 часа",
            "Погода отличная — может, прогуляемся по парку в {0}? ☕"),
        new(DatePreferenceCode.ActiveOutdoors, "Велопрогулка",
            "Аренда велосипедов и небольшой маршрут в {0}.", 20m, "2 часа",
            "Как насчёт покататься на велосипедах в {0}?"),
        new(DatePreferenceCode.CalmHangout, "Уютное кафе",
            "Спокойный вечер за чашкой чая или кофе в тихом кафе в {0}.", 25m, "1-2 часа",
            "Хочешь посидеть в уютном кафе в {0}?"),
        new(DatePreferenceCode.CalmHangout, "Вечер в чайной",
            "Разговор за чайной церемонией в одной из чайных в {0}.", 20m, "1.5 часа",
            "Есть отличная чайная в {0}, заглянем?"),
        new(DatePreferenceCode.QuizzesBoardGames, "Настольные игры в антикафе",
            "Пара часов за настольными играми в антикафе в {0}.", 15m, "2 часа",
            "Как насчёт настолок в антикафе в {0}?"),
        new(DatePreferenceCode.QuizzesBoardGames, "Квиз-вечер",
            "Командный квиз-вечер в одном из баров в {0}.", 15m, "2-3 часа",
            "В {0} сегодня квиз — пойдём в команде?"),
        new(DatePreferenceCode.SomethingNew, "Мастер-класс",
            "Совместный мастер-класс (гончарка, кулинария или рисование) в {0}.", 40m, "2 часа",
            "Нашла интересный мастер-класс в {0}, пойдём вместе?"),
        new(DatePreferenceCode.SomethingNew, "Новая кухня",
            "Ужин в ресторане с кухней, которую ни один из вас ещё не пробовал, в {0}.", 35m, "2 часа",
            "Давай попробуем что-то новое — ресторан незнакомой кухни в {0}?"),
        new(null, "Прогулка по центру",
            "Прогулка по центру, в {0}, с остановкой на кофе.", 10m, "1-2 часа",
            "Погуляем по центру, в {0}?"),
        new(null, "Кофе и разговоры",
            "Встреча в кофейне в {0} — просто поговорить и узнать друг друга лучше.", 15m, "1 час",
            "Может, встретимся за кофе в {0}?"),
    ];

    /// <summary>
    /// Сначала берутся шаблоны по пересечению предпочтений участников (не более одного на предпочтение — для
    /// разнообразия), затем список добирается шаблонами без привязки к предпочтению, а если и их не хватило до
    /// <paramref name="minCount"/> — оставшимися из каталога, чтобы список не оказался пустым. Бюджет фильтрует
    /// каталог, только если запрошена валюта <see cref="DefaultCurrency"/>: конвертации валют в заглушке нет, а
    /// каталог хранит цены только в BYN. По той же причине ответ всегда подписан <see cref="DefaultCurrency"/>,
    /// даже если запрошена другая валюта, — иначе BYN-цифра выдавалась бы за сумму в чужой валюте (например,
    /// «$40» вместо 40 BYN), что вводит пользователя в заблуждение сильнее, чем просто отсутствие конвертации.
    /// </summary>
    public static IReadOnlyList<DateIdeaItemResult> Pick(
        IReadOnlySet<DatePreferenceCode> sharedPreferenceCodes, decimal? maxBudget, string city, string requestedCurrency, int maxCount, int minCount)
    {
        var withinBudget = maxBudget is not null && requestedCurrency.Equals(DefaultCurrency, StringComparison.OrdinalIgnoreCase)
            ? Templates.Where(t => t.EstimatedCostByn <= maxBudget.Value).ToList()
            : Templates.ToList();

        var matched = withinBudget
            .Where(t => t.PreferenceCode is not null && sharedPreferenceCodes.Contains(t.PreferenceCode.Value))
            .DistinctBy(t => t.PreferenceCode)
            .Take(maxCount)
            .ToList();

        var picked = new List<Template>(matched);
        foreach (var template in withinBudget.Where(t => t.PreferenceCode is null))
        {
            if (picked.Count >= maxCount)
            {
                break;
            }

            picked.Add(template);
        }

        foreach (var template in withinBudget.Except(picked))
        {
            if (picked.Count >= minCount)
            {
                break;
            }

            picked.Add(template);
        }

        return picked.Take(maxCount).Select(t => t.ToResult(city)).ToList();
    }

    private sealed record Template(
        DatePreferenceCode? PreferenceCode,
        string Title,
        string DescriptionTemplate,
        decimal EstimatedCostByn,
        string EstimatedDuration,
        string InviteTextTemplate)
    {
        public DateIdeaItemResult ToResult(string city) => new(
            Title,
            string.Format(DescriptionTemplate, city),
            EstimatedCostByn,
            DefaultCurrency,
            EstimatedDuration,
            string.Format(InviteTextTemplate, city));
    }
}
