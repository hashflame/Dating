namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда мэтча с таким id нет — в том числе когда он существует, но запрашивающий пользователь
/// не его участник (запрос всегда ищет мэтч в паре (matchId, userId), так что чужой мэтч не отличим от
/// несуществующего — IDOR-защита, T-7.2, по аналогии с <see cref="PhotoNotFoundException"/>).
/// </summary>
public sealed class MatchNotFoundException(Guid matchId)
    : BlizkaDomainException(
        "MATCH_NOT_FOUND",
        $"Match {matchId} was not found.",
        new Dictionary<string, object?> { ["matchId"] = matchId })
{
    public Guid MatchId { get; } = matchId;
}
