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

    /// <summary>
    /// Секция «new» (T-7.1) — <c>Status = Active</c>, контакт ещё не открыт, свежие сверху. <c>User1</c>/<c>User2</c>
    /// загружены с фото, интересами и городом — для проекции второго участника и подсчёта совместимости (бейдж <c>fire</c>).
    /// </summary>
    Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Секция «waitingForMessage» (T-7.1) — контакт открыт, подтверждения отправки (<c>MessageSentCheckAt</c>) ещё нет, свежие по <c>ContactUnlockedAt</c> сверху.</summary>
    Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Секция «archived» (T-7.1) — <c>Status = Archived</c>.</summary>
    Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken);
}
