using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Feed;

/// <summary>Результат <c>GET</c>/<c>PATCH /api/feed/filters</c> (T-5.4).</summary>
public sealed record FeedFiltersResult(
    ShowGenderPreference ShowGender,
    int AgeMin,
    int AgeMax,
    int MaxDistanceKm,
    IReadOnlyCollection<DatingGoal> DatingGoals,
    bool RequireFilledProfile,
    int? ActiveWithinDays,
    bool RequirePhoto,
    bool VerifiedOnly,
    bool NonSmoker,
    bool NonDrinker,
    bool NoChildren)
{
    public static FeedFiltersResult From(UserFilter filter) => new(
        filter.ShowGender,
        filter.AgeMin,
        filter.AgeMax,
        filter.MaxDistanceKm,
        filter.DatingGoals,
        filter.RequireFilledProfile,
        filter.ActiveWithinDays,
        filter.RequirePhoto,
        filter.VerifiedOnly,
        filter.NonSmoker,
        filter.NonDrinker,
        filter.NoChildren);

    /// <summary>MVP-дефолты для пользователя, ещё ни разу не сохранявшего фильтры (см. <see cref="UserFilterDefaults"/>).</summary>
    public static FeedFiltersResult Default(ShowGenderPreference showGender) => new(
        showGender,
        UserFilterDefaults.AgeMin,
        UserFilterDefaults.AgeMax,
        UserFilterDefaults.MaxDistanceKm,
        [],
        RequireFilledProfile: false,
        ActiveWithinDays: null,
        RequirePhoto: false,
        VerifiedOnly: false,
        NonSmoker: false,
        NonDrinker: false,
        NoChildren: false);
}

/// <summary>Возрастной диапазон фильтра — сгруппирован, чтобы Min/Max нельзя было обновить порознь и рассинхронизировать.</summary>
public sealed record FeedAgeRange(int Min, int Max);
