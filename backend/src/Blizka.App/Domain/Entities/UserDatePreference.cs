namespace Blizka.App.Domain.Entities;

public sealed class UserDatePreference
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    public Guid DatePreferenceId { get; set; }

    public DatePreference? DatePreference { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
