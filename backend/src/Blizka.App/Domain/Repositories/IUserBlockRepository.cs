using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

public interface IUserBlockRepository
{
    /// <summary>Есть ли блокировка в конкретном направлении (blockerUserId → blockedUserId).</summary>
    Task<bool> ExistsAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken);

    /// <summary>
    /// Заблокирован ли хотя бы один из пары другим, независимо от направления — используется, чтобы скрыть
    /// пару друг от друга в ленте (T-16.2) и запретить свайп в обе стороны.
    /// </summary>
    Task<bool> ExistsEitherDirectionAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken);

    /// <summary>Список заблокированных текущим пользователем, от самой свежей блокировки к старым.</summary>
    Task<IReadOnlyList<UserBlock>> GetBlockedByUserAsync(Guid blockerUserId, CancellationToken cancellationToken);

    Task AddAsync(UserBlock block, CancellationToken cancellationToken);

    Task RemoveAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
