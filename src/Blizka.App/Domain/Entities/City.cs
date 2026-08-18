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

    public DateTimeOffset CreatedAt { get; set; }
}
