namespace Blizka.App.Domain.Entities;

public sealed class UserInterest
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Guid InterestId { get; set; }

    public Interest? Interest { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
