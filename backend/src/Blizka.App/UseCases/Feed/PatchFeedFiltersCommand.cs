using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Feed;

/// <summary>
/// <c>PATCH /api/feed/filters</c> (T-5.4) — частичное обновление: поля со значением <c>null</c> оставляют
/// уже сохранённые данные без изменений (кроме <see cref="AgeRange"/> — обновляется только целиком).
/// </summary>
/// <param name="ActiveWithinDays">
/// <c>null</c> — не трогать сохранённое значение; <see cref="ClearActiveWithinDays"/> (<c>-1</c>) — выключить
/// фильтр (вернуть <c>null</c>); положительное число — включить/изменить порог. Обычный <c>bool?</c>-паттерн
/// ("null = не трогать") здесь не подходит, т.к. у самого поля есть собственное значимое состояние "выключено" —
/// без отдельного сигнала клиент не смог бы отличить "не присылаю это поле" от "хочу выключить фильтр".
/// </param>
public sealed record PatchFeedFiltersCommand(
    Guid UserId,
    ShowGenderPreference? ShowGender,
    FeedAgeRange? AgeRange,
    int? MaxDistanceKm,
    IReadOnlyCollection<DatingGoal>? DatingGoals,
    bool? RequireFilledProfile,
    int? ActiveWithinDays,
    bool? RequirePhoto,
    bool? VerifiedOnly,
    bool? NonSmoker,
    bool? NonDrinker,
    bool? NoChildren) : IRequest<FeedFiltersResult>
{
    /// <summary>Сентинел для <see cref="ActiveWithinDays"/>: явно выключить фильтр активности через PATCH.</summary>
    public const int ClearActiveWithinDays = -1;
}
