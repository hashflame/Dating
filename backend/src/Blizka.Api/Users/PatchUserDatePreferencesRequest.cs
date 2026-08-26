using Blizka.Api.DatePreferences;
using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.DatePreferences;

namespace Blizka.Api.Users;

/// <summary>
/// Тело <c>PATCH /api/users/me/date-preferences</c> (T-9.3) — задаёт полный набор предпочтений по формату
/// свидания пользователя (замена, как и <c>interestIds</c> в <see cref="PatchUserInterestsRequest"/>).
/// </summary>
public sealed record PatchUserDatePreferencesRequest(DatePreferenceCode[]? Preferences);

/// <param name="Profile">Профиль после применения патча.</param>
/// <param name="SparksAwarded">Бонус за впервые достигнутый порог ProfileCompleteness этим вызовом (0, если порог не достигнут).</param>
/// <param name="Preferences">Итоговый набор предпочтений пользователя после патча.</param>
public sealed record PatchUserDatePreferencesResponse(UserMeResponse Profile, int SparksAwarded, DatePreferenceDto[] Preferences)
{
    public static PatchUserDatePreferencesResponse From(PatchUserDatePreferencesResult result) => new(
        UserMeResponse.From(result.Profile), result.SparksAwarded, [.. result.Preferences.Select(DatePreferenceDto.From)]);
}
