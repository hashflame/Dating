using Blizka.App.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Blizka.App.Domain.Entities;

public sealed class City
{
    public Guid Id { get; set; }

    public string NameRu { get; set; } = string.Empty;

    public string NameBe { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public string Country { get; set; } = "BY";

    public Point Coordinates { get; set; } = null!;

    public bool IsOpen { get; set; } = true;

    /// <summary>Область/страна для отображения в поиске (spec 002, B11) — не локализуется по языку интерфейса.</summary>
    public string? Region { get; set; }

    public CityType Type { get; set; } = CityType.City;

    public DateTimeOffset CreatedAt { get; set; }
}
