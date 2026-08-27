using Blizka.App.UseCases.Likes;

namespace Blizka.Api.Likes;

/// <summary>
/// Ответ <c>GET /api/likes/incoming</c> (T-6.1, spec.md 7.1). До разблокировки (<c>revealed: false</c>) заполнен
/// только <c>preview</c>, после — только <c>users</c>.
/// </summary>
/// <param name="Count">Сколько человек лайкнули текущего пользователя (без уже смэтченных).</param>
/// <param name="Revealed">Список уже разблокирован навсегда (<c>User.LikesRevealed</c>).</param>
/// <param name="UnlockCost">Стоимость разблокировки в зорках (<c>Sparks:LikesRevealCost</c>).</param>
/// <param name="Preview">Заблюренные превью главных фото — только пока <c>revealed: false</c>.</param>
/// <param name="Users">Полный список лайкнувших — только когда <c>revealed: true</c>.</param>
public sealed record IncomingLikesResponse(int Count, bool Revealed, int UnlockCost, LikePreviewDto[]? Preview, LikeUserDto[]? Users)
{
    public static IncomingLikesResponse From(IncomingLikesResult result) => new(
        result.Count,
        result.Revealed,
        result.UnlockCost,
        result.Revealed ? null : result.BlurredPreviewPhotos.Select(LikePreviewDto.From).ToArray(),
        result.Revealed ? result.Users.Select(LikeUserDto.From).ToArray() : null);
}

/// <param name="BlurredPhotoUrl">Data URI с заблюренным JPEG — сгенерирован на лету, не хранится отдельным вариантом фото.</param>
public sealed record LikePreviewDto(string BlurredPhotoUrl)
{
    public static LikePreviewDto From(byte[] blurredJpegBytes) =>
        new($"data:image/jpeg;base64,{Convert.ToBase64String(blurredJpegBytes)}");
}

/// <summary>Пользователь-участник лайка (S-21) — используется во всех трёх ответах T-6.1.</summary>
public sealed record LikeUserDto(Guid UserId, string Name, int? Age, string? MainPhotoUrl)
{
    public static LikeUserDto From(LikeUserResult result) => new(result.UserId, result.Name, result.Age, result.MainPhotoUrl);
}

/// <summary>Ответ <c>GET /api/likes/outgoing</c> (T-6.1) — кого лайкнул текущий пользователь, без мэтча.</summary>
public sealed record OutgoingLikesResponse(int Count, LikeUserDto[] Users)
{
    public static OutgoingLikesResponse From(OutgoingLikesResult result) =>
        new(result.Count, result.Users.Select(LikeUserDto.From).ToArray());
}

/// <summary>Ответ <c>POST /api/likes/incoming/reveal</c> (T-6.1, spec.md 7.2).</summary>
/// <param name="SparksSpent">0 при повторном вызове после уже состоявшейся разблокировки.</param>
/// <param name="SparksBalance">Баланс зорок текущего пользователя после операции.</param>
/// <param name="Users">Полный список лайкнувших после разблокировки.</param>
public sealed record RevealIncomingLikesResponse(int SparksSpent, int SparksBalance, LikeUserDto[] Users)
{
    public static RevealIncomingLikesResponse From(RevealIncomingLikesResult result) =>
        new(result.SparksSpent, result.SparksBalance, result.Users.Select(LikeUserDto.From).ToArray());
}
