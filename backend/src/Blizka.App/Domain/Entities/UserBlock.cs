namespace Blizka.App.Domain.Entities;

/// <summary>Блокировка одним пользователем другого (T-16.2) — заблокированный не появляется в ленте и не может свайпать блокирующего.</summary>
public sealed class UserBlock
{
    public Guid Id { get; set; }

    public Guid BlockerUserId { get; set; }

    public User? BlockerUser { get; set; }

    public Guid BlockedUserId { get; set; }

    public User? BlockedUser { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
