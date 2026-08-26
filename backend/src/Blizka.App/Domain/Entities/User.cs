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

    public string? TelegramUsername { get; set; }

    public bool IsVerified { get; set; }

    /// <summary>Причина бана — до T-17.1 проставляется модератором вручную прямой записью в БД (spec 002, B2).</summary>
    public string? BanReason { get; set; }

    /// <summary>Срок бана — <c>null</c> означает бессрочный бан (spec 002, B2).</summary>
    public DateTimeOffset? BannedUntil { get; set; }

    public int SparksBalance { get; set; }

    public int ProfileCompleteness { get; set; }

    /// <summary>
    /// Защита от повторного начисления регистрационного бонуса (по образцу CompletenessBonus60/80/100AwardedAt) —
    /// без неё сброс онбординга через DELETE /api/onboarding/draft и повторный проход до Complete начислял бы
    /// RegistrationBonus заново на каждый круг.
    /// </summary>
    public DateTimeOffset? RegistrationBonusAwardedAt { get; set; }

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

    public ICollection<UserDatePreference> UserDatePreferences { get; set; } = [];
}
