using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

/// <summary>
/// Доступ к данным для списков лайков (T-6.1) — поверх <c>Swipe</c>/<c>Match</c>, отдельно от
/// <see cref="ISwipeRepository"/> (тот отвечает за мутации свайпов, этот — только за чтение списков).
/// Счётчики и превью (<see cref="CountIncomingAsync"/>, <see cref="GetIncomingPreviewAsync"/>) по-прежнему
/// исключают пары, уже образовавшие <c>Match</c>. Полные списки (<see cref="GetIncomingAsync"/>,
/// <see cref="GetOutgoingAsync"/>) их не исключают — вместо этого помечают через <see cref="LikeEntry.MatchId"/>,
/// чтобы смэтченный собеседник не пропадал молча из уже разблокированного списка (баг из тикета ClickUp).
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

    /// <summary>Все входящие лайкнувшие, с фото — полный список после разблокировки. Смэтченные включены (см. <see cref="LikeEntry.MatchId"/>).</summary>
    Task<IReadOnlyList<LikeEntry>> GetIncomingAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Все, кого лайкнул пользователь, с фото. Смэтченные включены (см. <see cref="LikeEntry.MatchId"/>).</summary>
    Task<IReadOnlyList<LikeEntry>> GetOutgoingAsync(Guid userId, CancellationToken cancellationToken);
}

/// <summary>
/// Пользователь-участник лайка вместе с моментом свайпа — для сортировки списков лайков по свежести (T-6.1).
/// </summary>
/// <param name="MatchId">Id уже образованного мэтча с этой парой, если есть, иначе <c>null</c> (T-6.1 ClickUp-тикет:
/// смэтченные больше не исключаются из списков, а помечаются).</param>
public sealed record LikeEntry(User User, DateTimeOffset SwipedAt, Guid? MatchId = null);
