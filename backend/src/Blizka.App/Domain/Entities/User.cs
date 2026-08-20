using Blizka.App.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Blizka.App.Domain.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public long TelegramId { get; set; }

    public UserStatus Status { get; set; } = UserStatus.New;

    public string Name { get; set; } = string.Empty;

    public DateOnly BirthDate { get; set; }

    public Gender Gender { get; set; }

    public Guid? CityId { get; set; }

    public City? City { get; set; }

    public Point? Coordinates { get; set; }

    public DatingGoal? DatingGoal { get; set; }

    public string? Bio { get; set; }

    public int? Height { get; set; }

    public SmokingHabit? Smoking { get; set; }

    public DrinkingHabit? Drinking { get; set; }

    public Chronotype? Chronotype { get; set; }

    // Нет источника данных нигде в онбординге/профиле (T-5.4: нужно для фильтра noChildren, заполнять пока
    // негде) — null означает "не указано", а не "детей нет", отдельно от false.
    public bool? HasChildren { get; set; }

    public string[] Prompts { get; set; } = [];

    public string? InstagramHandle { get; set; }

    public string? VoiceIntroUrl { get; set; }

    public bool IsVerified { get; set; }

    public int SparksBalance { get; set; }

    public int ProfileCompleteness { get; set; }

    public DateTimeOffset? CompletenessBonus60AwardedAt { get; set; }

    public DateTimeOffset? CompletenessBonus80AwardedAt { get; set; }

    public DateTimeOffset? CompletenessBonus100AwardedAt { get; set; }

    public bool LikesRevealed { get; set; }

    public string Locale { get; set; } = "ru";

    public DateTimeOffset? LastActiveAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public ICollection<Photo> Photos { get; set; } = [];

    public ICollection<UserInterest> UserInterests { get; set; } = [];
}
