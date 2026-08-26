using Blizka.App.UseCases.Matches;

namespace Blizka.Api.Matches;

/// <summary>Ответ <c>GET /api/matches</c> (T-7.1, spec.md 8.1) — три секции: новые, ждут сообщения, архив.</summary>
public sealed record MatchesResponse(NewMatchDto[] New, WaitingMatchDto[] WaitingForMessage, ArchivedMatchDto[] Archived)
{
    public static MatchesResponse From(MatchesResult result) => new(
        result.New.Select(NewMatchDto.From).ToArray(),
        result.WaitingForMessage.Select(WaitingMatchDto.From).ToArray(),
        result.Archived.Select(ArchivedMatchDto.From).ToArray());
}

/// <summary>Второй участник мэтча (S-30) — используется во всех трёх секциях T-7.1.</summary>
public sealed record MatchUserDto(Guid UserId, string Name, int Age, string? MainPhotoUrl)
{
    public static MatchUserDto From(MatchUserResult result) => new(result.UserId, result.Name, result.Age, result.MainPhotoUrl);
}

/// <param name="MatchId">Идентификатор мэтча.</param>
/// <param name="User">Второй участник мэтча.</param>
/// <param name="MatchedAt">Момент образования мэтча.</param>
/// <param name="ContactCost">Стоимость открытия контакта в зорках.</param>
/// <param name="WritesFirst">Партнёр запретил себе писать первым в Telegram (S-51) — MVP: всегда <c>false</c>, T-16.1 ещё не реализована.</param>
/// <param name="Badge"><c>"fire"</c> при высокой совместимости, иначе <c>null</c>.</param>
public sealed record NewMatchDto(Guid MatchId, MatchUserDto User, DateTimeOffset MatchedAt, int ContactCost, bool WritesFirst, string? Badge)
{
    public static NewMatchDto From(NewMatchResult result) => new(
        result.MatchId, MatchUserDto.From(result.User), result.MatchedAt, result.ContactCost, result.WritesFirst, result.Badge);
}

public sealed record WaitingMatchDto(Guid MatchId, MatchUserDto User, DateTimeOffset ContactOpenedAt, string Badge)
{
    public static WaitingMatchDto From(WaitingMatchResult result) => new(
        result.MatchId, MatchUserDto.From(result.User), result.ContactOpenedAt, result.Badge);
}

/// <param name="MatchId">Идентификатор мэтча.</param>
/// <param name="User">Второй участник мэтча.</param>
/// <param name="ArchivedAt">Момент архивации.</param>
/// <param name="Reason">Причина архивации: <c>"no_activity_7_days"</c> (автоархивация джобой ArchiveStaleMatches, T-7.4) или <c>"manual"</c> (ручной <c>POST /archive</c> раньше срока протухания).</param>
public sealed record ArchivedMatchDto(Guid MatchId, MatchUserDto User, DateTimeOffset ArchivedAt, string Reason)
{
    public static ArchivedMatchDto From(ArchivedMatchResult result) => new(
        result.MatchId, MatchUserDto.From(result.User), result.ArchivedAt, result.Reason);
}

/// <summary>Ответ <c>GET /api/matches/{matchId}</c> (T-7.2, spec.md 8.2) — детальная карточка мэтча.</summary>
public sealed record MatchHubResponse(
    Guid MatchId,
    MatchHubUserDto User,
    MatchHubCompatibilityDto Compatibility,
    string ContactStatus,
    int ContactCost,
    MatchHubFeaturesDto Features)
{
    public static MatchHubResponse From(MatchHubResult result) => new(
        result.MatchId,
        MatchHubUserDto.From(result.User),
        new MatchHubCompatibilityDto(result.Compatibility.Score, result.Compatibility.Details),
        result.ContactStatus,
        result.ContactCost,
        MatchHubFeaturesDto.From(result.Features));
}

/// <summary><c>TelegramUsername</c> — только после оплаты (<c>unlock</c>, T-7.3), до этого <c>null</c> (spec.md 8.2).</summary>
public sealed record MatchHubUserDto(
    Guid UserId, string Name, int Age, string City, DateTimeOffset? LastActive, string? TelegramUsername, string? MainPhotoUrl)
{
    public static MatchHubUserDto From(MatchHubUserResult result) => new(
        result.UserId, result.Name, result.Age, result.CityName, result.LastActiveAt, result.TelegramUsername, result.MainPhotoUrl);
}

public sealed record MatchHubCompatibilityDto(int Score, string Details);

/// <summary>QuestionOfDay (T-11.1) и DateIdea (T-12.1) доступны для всех мэтчей; Minigame/StaleConversation остаются MVP-заглушкой (T-7.2) — <c>available: false</c> до T-14.1/T-15.1.</summary>
public sealed record MatchHubFeaturesDto(
    FeatureAvailabilityDto QuestionOfDay, FeatureAvailabilityDto Minigame, FeatureAvailabilityDto DateIdea, FeatureAvailabilityDto StaleConversation)
{
    public static MatchHubFeaturesDto From(MatchHubFeaturesResult result) => new(
        new FeatureAvailabilityDto(result.QuestionOfDay.Available),
        new FeatureAvailabilityDto(result.Minigame.Available),
        new FeatureAvailabilityDto(result.DateIdea.Available),
        new FeatureAvailabilityDto(result.StaleConversation.Available));
}

public sealed record FeatureAvailabilityDto(bool Available);

/// <summary>Ответ <c>POST /api/matches/{matchId}/unlock</c> (T-7.3, spec.md 9.1).</summary>
public sealed record UnlockContactResponse(string? TelegramUsername, string? DeepLink, int SparksSpent, int SparksBalance)
{
    public static UnlockContactResponse From(UnlockContactResult result) => new(
        result.TelegramUsername, result.DeepLink, result.SparksSpent, result.SparksBalance);
}
