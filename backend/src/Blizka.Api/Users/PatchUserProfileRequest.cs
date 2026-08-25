using Blizka.Api.Onboarding;
using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Users;

namespace Blizka.Api.Users;

/// <summary>
/// Тело <c>PATCH /api/users/me/profile</c> (T-9.1) — частичное обновление: не переданное (<c>null</c>) поле
/// оставляет уже сохранённое значение без изменений, тот же принцип, что и у <c>PATCH /api/feed/filters</c>.
/// </summary>
public sealed record PatchUserProfileRequest(
    string? Name,
    string? Bio,
    int? Height,
    SmokingHabit? Smoking,
    DrinkingHabit? Drinking,
    Chronotype? Chronotype,
    string[]? Prompts,
    DatingGoal? DatingGoal);

/// <param name="Profile">Профиль после применения патча.</param>
/// <param name="SparksAwarded">Бонус за впервые достигнутый порог ProfileCompleteness этим вызовом (0, если порог не достигнут).</param>
public sealed record PatchUserProfileResponse(UserMeResponse Profile, int SparksAwarded)
{
    public static PatchUserProfileResponse From(PatchUserProfileResult result) =>
        new(UserMeResponse.From(result.Profile), result.SparksAwarded);
}
