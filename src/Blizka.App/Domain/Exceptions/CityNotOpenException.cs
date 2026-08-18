namespace Blizka.App.Domain.Exceptions;

/// <summary>Thrown when an action requires a city that isn't open for the product yet (see <c>City.IsOpen</c>).</summary>
public sealed class CityNotOpenException(Guid cityId)
    : BlizkaDomainException(
        "CITY_NOT_OPEN",
        $"City {cityId} is not open yet.",
        new Dictionary<string, object?> { ["cityId"] = cityId })
{
    public Guid CityId { get; } = cityId;
}
