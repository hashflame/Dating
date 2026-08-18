namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается, когда действие требует города, который ещё не открыт для продукта (см. <c>City.IsOpen</c>).</summary>
public sealed class CityNotOpenException(Guid cityId)
    : BlizkaDomainException(
        "CITY_NOT_OPEN",
        $"City {cityId} is not open yet.",
        new Dictionary<string, object?> { ["cityId"] = cityId })
{
    public Guid CityId { get; } = cityId;
}
