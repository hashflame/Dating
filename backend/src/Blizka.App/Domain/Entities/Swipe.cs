using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

public sealed class Swipe
{
    public Guid Id { get; set; }

    public Guid FromUserId { get; set; }

    public User? FromUser { get; set; }

    public Guid ToUserId { get; set; }

    public User? ToUser { get; set; }

    public SwipeType Type { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UndoneAt { get; set; }
}
