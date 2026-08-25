using Blizka.Api.Interests;
using Blizka.App.UseCases.Interests;

namespace Blizka.Api.Users;

/// <summary>
/// Тело <c>PATCH /api/users/me/interests</c> (T-9.2) — задаёт полный набор интересов пользователя (замена).
/// <c>InterestIds</c> — уже существующие в каталоге интересы; <c>CustomInterests</c> — названия новых
/// кастомных интересов, которых ещё нет в каталоге (см. <see cref="PatchUserInterestsCommand"/> для деталей
/// расхождения с буквальным контрактом decomposition.md).
/// </summary>
public sealed record PatchUserInterestsRequest(Guid[]? InterestIds, string[]? CustomInterests);

/// <param name="Profile">Профиль после применения патча.</param>
/// <param name="SparksAwarded">Бонус за впервые достигнутый порог ProfileCompleteness этим вызовом (0, если порог не достигнут).</param>
/// <param name="Interests">Итоговый набор интересов пользователя после патча.</param>
public sealed record PatchUserInterestsResponse(UserMeResponse Profile, int SparksAwarded, InterestDto[] Interests)
{
    public static PatchUserInterestsResponse From(PatchUserInterestsResult result) => new(
        UserMeResponse.From(result.Profile), result.SparksAwarded, [.. result.Interests.Select(InterestDto.From)]);
}
