using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

/// <summary>Доступ к данным для лайков/дизлайков/суперлайков и мэтчинга (T-5.2).</summary>
public interface ISwipeRepository
{
    /// <summary>Есть ли уже активный (не отменённый) свайп этой пары в эту сторону.</summary>
    Task<bool> ExistsActiveAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken);

    /// <summary>Есть ли встречный активный лайк/суперлайк (toUserId → fromUserId) — условие для создания мэтча.</summary>
    Task<bool> HasActiveMutualLikeAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken);

    Task AddAsync(Swipe swipe, CancellationToken cancellationToken);

    /// <summary>
    /// Сохраняет все изменения, отслеживаемые контекстом (свайп, при мэтче — Match, при суперлайке —
    /// списание зорок/SparkTransaction), одной транзакцией. Переинтерпретирует конфликты уникальных
    /// индексов/конкурентного обновления баланса в <see cref="ConcurrentSwipeCreationException"/> и
    /// <see cref="ConcurrentUserUpdateException"/> — вызывающий код решает, как их показать клиенту.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
