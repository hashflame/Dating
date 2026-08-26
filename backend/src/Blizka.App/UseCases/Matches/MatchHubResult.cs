namespace Blizka.App.UseCases.Matches;

/// <summary>Результат <c>GET /api/matches/{matchId}</c> (T-7.2, spec.md 8.2) — детальная карточка мэтча.</summary>
/// <param name="ContactStatus"><c>"locked"</c> | <c>"unlocked"</c> — <c>"writes_first_only"</c> недостижим, пока T-16.1 (настройки приватности) не реализована, по аналогии с <see cref="NewMatchResult.WritesFirst"/> в T-7.1.</param>
/// <param name="ContactCost">Стоимость открытия контакта в зорках (<c>Sparks:ContactUnlockCost</c>) — возвращается независимо от <paramref name="ContactStatus"/>.</param>
/// <param name="Features">QuestionOfDay (T-11.1) и DateIdea (T-12.1, MVP-заглушка) реализованы и доступны во всех мэтчах; Minigame/StaleConversation (T-14.1/T-15.1) ещё нет — <c>available: false</c>.</param>
public sealed record MatchHubResult(
    Guid MatchId,
    MatchHubUserResult User,
    MatchHubCompatibilityResult Compatibility,
    string ContactStatus,
    int ContactCost,
    MatchHubFeaturesResult Features);

public sealed record MatchHubUserResult(
    Guid UserId, string Name, int Age, string CityName, DateTimeOffset? LastActiveAt, string? TelegramUsername, string? MainPhotoUrl);

/// <param name="Details">Текстовое описание совпадений — decomposition.md/spec.md не задают шаблон, сформировано из совпавших интересов и целей/верификации <see cref="MatchCompatibilityDescriber"/> (решение продукта при уточнении T-7.2).</param>
public sealed record MatchHubCompatibilityResult(int Score, string Details);

public sealed record MatchHubFeaturesResult(
    FeatureAvailabilityResult QuestionOfDay,
    FeatureAvailabilityResult Minigame,
    FeatureAvailabilityResult DateIdea,
    FeatureAvailabilityResult StaleConversation);

public sealed record FeatureAvailabilityResult(bool Available);
