namespace Blizka.App.UseCases.Likes;

/// <summary>
/// Результат <c>GET /api/likes/incoming</c> (T-6.1). До разблокировки (<paramref name="Revealed"/>: <c>false</c>)
/// заполнен только <paramref name="BlurredPreviewPhotos"/> (заблюренные JPEG-байты главных фото лайкнувших,
/// <paramref name="Users"/> — пустой список); после разблокировки — наоборот.
/// </summary>
public sealed record IncomingLikesResult(
    int Count,
    bool Revealed,
    int UnlockCost,
    IReadOnlyList<byte[]> BlurredPreviewPhotos,
    IReadOnlyList<LikeUserResult> Users);
