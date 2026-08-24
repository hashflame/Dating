using Blizka.App.UseCases.Feed;

namespace Blizka.App.UseCases.Matches;

/// <summary>
/// Текстовое описание совпадений для <c>compatibility.details</c> в хабе мэтча (T-7.2). Шаблон не задан ни
/// decomposition.md, ни spec.md (там только пример готовой фразы) — решение продукта при уточнении задачи:
/// перечислить совпавшие интересы (те же <see cref="ScoredCandidate.SharedInterestIds"/>, что дают бейдж
/// <c>fire</c> в T-7.1) и отдельно отметить совпадение цели знакомства/обоюдную верификацию, а не пересчитывать
/// вес каждого фактора текстом.
/// </summary>
internal static class MatchCompatibilityDescriber
{
    public static string Describe(ScoredCandidate scored, IReadOnlyList<string> sharedInterestNames)
    {
        var parts = new List<string>();

        if (sharedInterestNames.Count > 0)
        {
            parts.Add($"Общие интересы: {string.Join(", ", sharedInterestNames)}");
        }

        if (scored.DatingGoalMatch)
        {
            parts.Add("Совпадает цель знакомства");
        }

        if (scored.BothVerified)
        {
            parts.Add("Оба профиля верифицированы");
        }

        return parts.Count == 0
            ? "Пока мало общих данных для сравнения."
            : string.Join(". ", parts) + ".";
    }
}
