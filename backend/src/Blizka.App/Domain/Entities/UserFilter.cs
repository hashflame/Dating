using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Entities;

/// <summary>
/// Персистентные фильтры ленты пользователя (T-5.4), 1:1 с <see cref="User"/>. Пока строка не создана (PATCH
/// ни разу не вызывался), <c>GET /api/feed</c> использует MVP-дефолты — см. <c>UserFilterDefaults</c>.
/// </summary>
public sealed class UserFilter
{
    public Guid UserId { get; set; }

    public User? User { get; set; }

    /// <summary>Кого показывать в ленте — из шага 2 онбординга (S-04) либо явно сохранено через PATCH.</summary>
    public ShowGenderPreference ShowGender { get; set; }

    public int AgeMin { get; set; }

    public int AgeMax { get; set; }

    public int MaxDistanceKm { get; set; }

    /// <summary>Пустой массив — цели знакомств не сужают выборку (показываются кандидаты с любой целью).</summary>
    public DatingGoal[] DatingGoals { get; set; } = [];

    public bool RequireFilledProfile { get; set; }

    /// <summary>Кандидат должен были активен за последние N дней. <c>null</c> — фильтр выключен.</summary>
    public int? ActiveWithinDays { get; set; }

    public bool RequirePhoto { get; set; }

    // Advanced (decomposition.md: "post-MVP toggle, но структуру заложить") — применяются в выборке сразу,
    // отдельного механизма feature-флагов в проекте нет.
    public bool VerifiedOnly { get; set; }

    public bool NonSmoker { get; set; }

    public bool NonDrinker { get; set; }

    public bool NoChildren { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
