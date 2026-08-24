namespace Blizka.App.UseCases.Likes;

/// <summary>
/// Результат <c>POST /api/likes/incoming/reveal</c> (T-6.1). <paramref name="SparksSpent"/> — 0 при повторном
/// вызове после уже состоявшейся разблокировки (идемпотентно, зорки повторно не списываются).
/// </summary>
public sealed record RevealIncomingLikesResult(int SparksSpent, int SparksBalance, IReadOnlyList<LikeUserResult> Users);
