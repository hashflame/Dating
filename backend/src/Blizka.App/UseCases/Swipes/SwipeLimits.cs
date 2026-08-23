namespace Blizka.App.UseCases.Swipes;

/// <summary>
/// Дневной лимит свайпов для бесплатных пользователей (spec 002, B3) — MVP-значение по аналогии со
/// скользящим окном отмен (T-5.3). Снимается для подписки «Безлимит» через <see cref="ISubscriptionChecker"/>
/// (точка расширения T-8.3, сама проверка подписки в этой спеке не реализуется).
/// </summary>
public static class SwipeLimits
{
    public const int DailyLimit = 50;
}
