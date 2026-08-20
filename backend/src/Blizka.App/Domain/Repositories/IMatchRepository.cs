using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

/// <summary>Доступ к данным мэтчей (T-5.2). Сохранение — через <see cref="ISwipeRepository.SaveChangesAsync"/>: свайп, мэтч и списание зорок пишутся одной транзакцией общего DbContext.</summary>
public interface IMatchRepository
{
    Task AddAsync(Match match, CancellationToken cancellationToken);

    /// <summary>Мэтч этой пары пользователей (порядок не важен, ищется по канонизированному (User1Id, User2Id)) — для отмены свайпа (T-5.3).</summary>
    Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken);

    /// <summary>Помечает мэтч на физическое удаление — коммит вместе с остальными изменениями через <see cref="ISwipeRepository.SaveChangesAsync"/> общего DbContext (T-5.3).</summary>
    void Remove(Match match);
}
