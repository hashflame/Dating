namespace Blizka.App.UseCases.Swipes;

/// <summary>
/// Три входа для начала общения после мэтча (spec.md 6.2, S-16) — фиксированный статичный набор, не сущность
/// БД: текст задан спекой буквально и одинаков для всех мэтчей, локалей be/en спека не даёт (как и у Prompts,
/// T-5.1 — оставлены как есть).
/// </summary>
internal static class IcebreakerCatalog
{
    public static IReadOnlyList<IcebreakerResult> Default { get; } =
    [
        new("question_of_day", "Вопрос дня", "10 секунд"),
        new("minigame", "Мини-игра", "2 минуты"),
        new("date_idea", "Идея", "1 тап"),
    ];
}
