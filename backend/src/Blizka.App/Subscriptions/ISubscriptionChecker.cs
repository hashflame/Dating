namespace Blizka.App.Subscriptions;

/// <summary>
/// Точка расширения под T-8.3 (подписка «Безлимит» снимает дневной лимит свайпов, spec 002 B3) —
/// сама проверка подписки не реализуется этой спекой, поэтому реализация нигде не регистрируется в
/// DI: встроенный контейнер подставит <c>null</c> в параметр конструктора с дефолтным значением
/// вместо падения на резолве несуществующего сервиса.
/// </summary>
public interface ISubscriptionChecker
{
    Task<bool> HasUnlimitedSwipesAsync(Guid userId, CancellationToken cancellationToken);
}
