namespace Blizka.App.Domain.Enums;

/// <summary>
/// Гранулярность населённого пункта в каталоге городов (spec 002, B11). <c>Town</c> — задел под
/// будущие агрогородки/посёлки (см. пример waitlist-города в spec.md §4.1); весь текущий сид
/// (T-4.1) состоит из городов, поэтому пока используется только <c>City</c>.
/// </summary>
public enum CityType
{
    City,
    Town,
}
