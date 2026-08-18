using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

public sealed class DatePreference
{
    public Guid Id { get; set; }

    public DatePreferenceCode Code { get; set; }

    public string NameRu { get; set; } = string.Empty;

    public string NameBe { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    public ICollection<UserDatePreference> UserDatePreferences { get; set; } = [];
}
