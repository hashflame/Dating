namespace Blizka.App.Domain.Entities;

public sealed class CityWaitlist
{
    public Guid CityId { get; set; }

    public City? City { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public bool NotifyOnOpen { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}
