namespace Blizka.App.UseCases.Likes;

/// <summary>Пользователь-участник лайка в списках T-6.1 — общая проекция для входящих, исходящих и разблокированных.</summary>
/// <param name="IsMatched">Уже образовался мэтч с этим человеком (тикет ClickUp: раньше такие молча пропадали из списка).</param>
/// <param name="MatchId">Id мэтча, если <paramref name="IsMatched"/> — чтобы фронт мог сразу открыть хаб.</param>
public sealed record LikeUserResult(Guid UserId, string Name, int? Age, string? MainPhotoUrl, bool IsMatched, Guid? MatchId);
