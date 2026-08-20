using Blizka.App.Domain.Enums;

namespace Blizka.App.UseCases.Feed;

/// <summary>
/// MVP-дефолты фильтров ленты (T-5.4) — применяются, пока у пользователя нет собственного сохранённого
/// <see cref="Domain.Entities.UserFilter"/> (бэкафилла для уже онбордившихся пользователей нет: заводится только
/// при завершении онбординга — см. <c>CompleteOnboardingCommandHandler</c> — или первым <c>PATCH /api/feed/filters</c>).
/// Значения, не заданные ни decomposition.md, ни spec.md, выбраны как MVP-приближение — как и остальные
/// пороги/веса в этой же задаче (см. <c>FeedCompatibilityScorer</c>).
/// </summary>
public static class UserFilterDefaults
{
    public const int AgeMin = 18;
    public const int AgeMax = 99;

    // Спекой не задано — приближение к типичному радиусу города, чтобы поведение без сохранённого
    // фильтра не отличалось резко от прежнего строгого совпадения CityId (T-5.1).
    public const int MaxDistanceKm = 50;

    // Синхронизировано с порогами бонусов ProfileCompleteness (T-2.3: 60/80/100%) — "заполненный профиль"
    // трактуется как прошедший первый из этих порогов, а не просто прошедший MVP-минимум онбординга (35%).
    public const int RequireFilledProfileMinCompleteness = 60;

    /// <summary>MVP-дефолт: показывать противоположный пол — единственные значения <see cref="Gender"/> сейчас Male/Female.</summary>
    public static ShowGenderPreference ResolveDefaultShowGender(Gender ownGender) =>
        ownGender == Gender.Male ? ShowGenderPreference.Female : ShowGenderPreference.Male;
}
