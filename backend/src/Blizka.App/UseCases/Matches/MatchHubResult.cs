namespace Blizka.App.UseCases.Matches;

/// <summary>Результат <c>GET /api/matches/{matchId}</c> (T-7.2, spec.md 8.2) — детальная карточка мэтча.</summary>
/// <param name="ContactStatus"><c>"locked"</c> | <c>"unlocked"</c> | <c>"writes_first_only"</c> (T-16.1: второй участник включил <c>blockIncomingMessages</c>).</param>
/// <param name="ContactCost">Стоимость открытия контакта в зорках (<c>Sparks:ContactUnlockCost</c>) — возвращается независимо от <paramref name="ContactStatus"/>.</param>
/// <param name="Features">QuestionOfDay.Available отражает реальную доступность вопроса на сегодня (T-11.1: <c>false</c>, пока джоба GenerateQuestionOfDay ни разу не отработала — согласовано с GET .../question-of-day); DateIdea (T-12.1, MVP-заглушка) доступна во всех мэтчах; Minigame/StaleConversation (T-14.1/T-15.1) ещё нет — <c>available: false</c>.</param>
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
