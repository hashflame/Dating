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

    /// <summary>
    /// То же условие, что у <see cref="GetNewAsync"/> (секция «new», T-7.1), но только счётчик — для бейджа
    /// непрочитанного (T-10.2, <c>GET /api/notifications/unread</c>), которому не нужны фото/интересы/город
    /// обоих участников, только количество.
    /// </summary>
    Task<int> CountNewAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Секция «waitingForMessage» (T-7.1) — контакт открыт, подтверждения отправки (<c>MessageSentCheckAt</c>) ещё нет, свежие по <c>ContactUnlockedAt</c> сверху.</summary>
    Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Секция «archived» (T-7.1) — <c>Status = Archived</c>.</summary>
    Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Хаб мэтча (T-7.2) — мэтч по <paramref name="matchId"/>, ищется сразу в паре с <paramref name="userId"/>
    /// (участник ли), а не отдельной проверкой после загрузки — чужой мэтч должен быть неотличим от
    /// несуществующего (IDOR-защита, см. <see cref="Blizka.App.Domain.Exceptions.MatchNotFoundException"/>). <c>User1</c>/<c>User2</c>
    /// загружены так же, как в <see cref="GetNewAsync"/> — фото, интересы и город нужны для проекции второго
    /// участника и подсчёта совместимости.
    /// </summary>
    Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Та же IDOR-защита, что у <see cref="GetByIdForUserAsync"/>, но без фото/интересов/предпочтений/города —
    /// для путей, которым от мэтча нужны только <c>Id</c>/<c>User1Id</c>/<c>User2Id</c> и базовые поля участников
    /// (например, <c>Locale</c>), а не полный профиль для подсчёта совместимости (T-11.1: вопрос дня и его архив
    /// не показывают собеседника целиком, в отличие от хаба мэтча, — гонять сюда четыре Include + AsSplitQuery
    /// ради проверки членства и локали не нужно). <c>AsNoTracking</c>, как и <see cref="GetByIdForUserAsync"/>.
    /// </summary>
    Task<Match?> GetByIdForUserBasicAsync(Guid matchId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// То же самое, что <see cref="GetByIdForUserAsync"/> (IDOR-защита: мэтч ищется сразу в паре с
    /// <paramref name="userId"/>), но для пути записи (T-7.3: открытие контакта, message-sent-check) — сущность
    /// отслеживается контекстом, чтобы мутации <c>Match</c>/<c>User.SparksBalance</c> сохранились через
    /// <see cref="SaveChangesAsync"/>. Грузит только базовые <c>User1</c>/<c>User2</c> без фото/интересов/города —
    /// они этому пути не нужны.
    /// </summary>
    Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Сохраняет все изменения, отслеживаемые контекстом (открытие контакта: <c>Match</c> + списание зорок с
    /// <c>User</c>/<c>SparkTransaction</c>; message-sent-check: только <c>Match</c>) одной транзакцией.
    /// И <c>User</c>, и <c>Match</c> защищены xmin-токеном конкурентности (см. <c>MatchConfiguration</c>) —
    /// конфликт на любом из них переинтерпретируется в <see cref="ConcurrentUserUpdateException"/>. Токен на
    /// <c>Match</c> нужен отдельно от токена на <c>User</c>: без него два разных участника мэтча, открывающие
    /// контакт почти одновременно, списали бы зорки каждый со своего баланса без конфликта, но тихо
    /// перезаписали бы друг друга в одной строке <c>Match</c> — вызывающий код решает, как показать эту
    /// ошибку клиенту.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Массовая автоархивация протухших мэтчей (T-7.4, фоновая джоба <c>ArchiveStaleMatches</c>, раз в 6 часов) —
    /// условие протухания: <see cref="Blizka.App.UseCases.Matches.MatchArchivalPolicy"/>. Коммитит напрямую через
    /// <c>ExecuteUpdateAsync</c>, без построчной загрузки сущностей и без прохода через <see cref="SaveChangesAsync"/> —
    /// побочных эффектов (списаний, уведомлений) у этой операции нет, только <c>Status</c>/<c>ArchivedAt</c>.
    /// Возвращает число заархивированных мэтчей — для лога джобы.
    /// </summary>
    Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken);
}
