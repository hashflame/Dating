using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Feed;

namespace Blizka.Api.Feed;

/// <summary>Ответ <c>GET</c>/<c>PATCH /api/feed/filters</c> (T-5.4, S-15).</summary>
public sealed record FeedFiltersResponse(
    ShowGenderPreference ShowGender,
    FeedAgeRangeDto AgeRange,
    int MaxDistanceKm,
    DatingGoal[] DatingGoals,
    bool RequireFilledProfile,
    int? ActiveWithinDays,
    bool RequirePhoto,
    bool VerifiedOnly,
    bool NonSmoker,
    bool NonDrinker,
    bool NoChildren)
{
    public static FeedFiltersResponse From(FeedFiltersResult result) => new(
        result.ShowGender,
        new FeedAgeRangeDto(result.AgeMin, result.AgeMax),
        result.MaxDistanceKm,
        [.. result.DatingGoals],
        result.RequireFilledProfile,
        result.ActiveWithinDays,
        result.RequirePhoto,
        result.VerifiedOnly,
        result.NonSmoker,
        result.NonDrinker,
        result.NoChildren);
}

public sealed record FeedAgeRangeDto(int Min, int Max);

/// <summary>
/// Тело <c>PATCH /api/feed/filters</c> — все поля необязательны, отсутствующие (<c>null</c>) оставляют
/// уже сохранённое значение без изменений; <see cref="AgeRange"/> обновляется только целиком (Min и Max вместе).
/// <c>ActiveWithinDays</c>: <c>null</c> — не трогать, <c>-1</c> — выключить фильтр активности, положительное
/// число — включить/изменить.
/// </summary>
public sealed record PatchFeedFiltersRequest(
    ShowGenderPreference? ShowGender,
    FeedAgeRangeDto? AgeRange,
    int? MaxDistanceKm,
    DatingGoal[]? DatingGoals,
    bool? RequireFilledProfile,
    int? ActiveWithinDays,
    bool? RequirePhoto,
    bool? VerifiedOnly,
    bool? NonSmoker,
    bool? NonDrinker,
    bool? NoChildren)
{
    public PatchFeedFiltersCommand ToCommand(Guid userId) => new(
        userId,
        ShowGender,
        AgeRange is null ? null : new FeedAgeRange(AgeRange.Min, AgeRange.Max),
        MaxDistanceKm,
        DatingGoals,
        RequireFilledProfile,
        ActiveWithinDays,
        RequirePhoto,
        VerifiedOnly,
        NonSmoker,
        NonDrinker,
        NoChildren);
}
