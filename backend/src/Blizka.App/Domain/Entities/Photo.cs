namespace Blizka.App.Domain.Entities;

public sealed class Photo
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User? User { get; set; }

    public string Url { get; set; } = string.Empty;

    public string ThumbnailUrl { get; set; } = string.Empty;

    public string MediumUrl { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsMain { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
