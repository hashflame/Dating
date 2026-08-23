namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается, когда города с таким id нет в каталоге (<c>GET /api/cities/{cityId}</c>, T-4.1).</summary>
public sealed class CityNotFoundException(Guid cityId)
    : BlizkaDomainException(
        "CITY_NOT_FOUND",
        $"City {cityId} was not found.",
        new Dictionary<string, object?> { ["cityId"] = cityId })
{
    public Guid CityId { get; } = cityId;
}
