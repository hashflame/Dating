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
/// <param name="Reason">Причина архивации — MVP: единственное описанное в спеке значение <c>"no_activity_7_days"</c> (T-7.4 ещё не реализована).</param>
public sealed record ArchivedMatchDto(Guid MatchId, MatchUserDto User, DateTimeOffset ArchivedAt, string Reason)
{
    public static ArchivedMatchDto From(ArchivedMatchResult result) => new(
        result.MatchId, MatchUserDto.From(result.User), result.ArchivedAt, result.Reason);
}
