namespace Blizka.App.Subscriptions;

/// <summary>
/// Точка расширения под T-8.3 (подписка «Безлимит»: снимает дневной лимит свайпов — spec 002 B3, и делает
/// открытие контакта бесплатным — decomposition.md T-7.3, «или 0, если подписка Безлимит») — сама проверка
/// подписки не реализуется этой спекой, поэтому реализация нигде не регистрируется в DI: встроенный
/// контейнер подставит <c>null</c> в параметр конструктора с дефолтным значением вместо падения на
/// резолве несуществующего сервиса.
/// </summary>
public interface ISubscriptionChecker
{
    Task<bool> HasUnlimitedSwipesAsync(Guid userId, CancellationToken cancellationToken);

    Task<bool> HasUnlimitedContactUnlocksAsync(Guid userId, CancellationToken cancellationToken);
}
