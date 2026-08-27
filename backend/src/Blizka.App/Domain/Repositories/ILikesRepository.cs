using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

/// <summary>
/// Доступ к данным для списков лайков (T-6.1) — поверх <c>Swipe</c>/<c>Match</c>, отдельно от
/// <see cref="ISwipeRepository"/> (тот отвечает за мутации свайпов, этот — только за чтение списков).
/// Во всех выборках пара, уже образовавшая <c>Match</c> (независимо от его статуса), исключается — «без мэтча»
/// по тексту decomposition.md для входящих, и симметрично для исходящих: смэтченные показываются в мэтчах,
/// не здесь.
/// </summary>
public interface ILikesRepository
{
    /// <summary>Сколько активных (не отменённых) входящих лайков/суперлайков у пользователя, за вычетом уже смэтченных пар.</summary>
    Task<int> CountIncomingAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// То же самое, что <see cref="CountIncomingAsync"/>, но только те, что появились после <paramref name="since"/>
    /// (<c>null</c> — все, как <see cref="CountIncomingAsync"/>) — для бейджа непрочитанного (T-10.2), который
    /// должен гаснуть после <c>POST /api/notifications/seen</c>, а не показывать вообще все входящие лайки
    /// (баг из тикета ClickUp: бейдж было невозможно погасить). Реализация по умолчанию (для тестовых фейков,
    /// которые её не переопределяют, по тому же образцу, что и <see cref="IUserRepository.GetByIdsAsync"/>)
    /// игнорирует <paramref name="since"/> — эффективна и корректна только настоящая EF-реализация в <c>Blizka.Data</c>.
    /// </summary>
    Task<int> CountIncomingSinceAsync(Guid userId, DateTimeOffset? since, CancellationToken cancellationToken) =>
        CountIncomingAsync(userId, cancellationToken);

    /// <summary>Самые свежие входящие лайкнувшие (без мэтча), с фото — источник заблюренного превью до разблокировки.</summary>
    Task<IReadOnlyList<LikeEntry>> GetIncomingPreviewAsync(Guid userId, int limit, CancellationToken cancellationToken);

    /// <summary>Все входящие лайкнувшие (без мэтча), с фото — полный список после разблокировки.</summary>
    Task<IReadOnlyList<LikeEntry>> GetIncomingAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Все, кого лайкнул пользователь (без мэтча), с фото.</summary>
    Task<IReadOnlyList<LikeEntry>> GetOutgoingAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>Пользователь-участник лайка вместе с моментом свайпа — для сортировки списков лайков по свежести (T-6.1).</summary>
public sealed record LikeEntry(User User, DateTimeOffset SwipedAt);
