namespace Blizka.App.UseCases.Likes;

/// <summary>Результат <c>GET /api/likes/outgoing</c> (T-6.1) — всегда полный список, разблокировки не требует.</summary>
public sealed record OutgoingLikesResult(int Count, IReadOnlyList<LikeUserResult> Users);
