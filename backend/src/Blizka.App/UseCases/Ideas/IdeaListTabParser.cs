using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Ideas;

/// <summary>Строки query-параметра <c>tab</c> ↔ <see cref="IdeaListTab"/> (T-19.1) — общее для валидатора и хендлера.</summary>
internal static class IdeaListTabParser
{
    public static readonly IReadOnlyCollection<string> AllowedValues = ["hot", "new", "inWork", "mine"];

    public static IdeaListTab Parse(string tab) => tab switch
    {
        "hot" => IdeaListTab.Hot,
        "new" => IdeaListTab.New,
        "inWork" => IdeaListTab.InWork,
        "mine" => IdeaListTab.Mine,
        _ => throw new ArgumentOutOfRangeException(nameof(tab), tab, "Unknown idea list tab — should have been rejected by GetIdeasQueryValidator."),
    };
}
