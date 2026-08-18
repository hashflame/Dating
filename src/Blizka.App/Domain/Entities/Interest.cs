using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

public sealed class Interest
{
    public Guid Id { get; set; }

    public InterestCategory Category { get; set; }

    public string NameRu { get; set; } = string.Empty;

    public string NameBe { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public bool IsCustom { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<UserInterest> UserInterests { get; set; } = [];
}
