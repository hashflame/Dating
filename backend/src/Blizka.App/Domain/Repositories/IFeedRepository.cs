using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

/// <summary>Доступ к данным для алгоритма ленты (T-5.1, T-5.4).</summary>
public interface IFeedRepository
{
    /// <summary>Текущий пользователь с городом и интересами — данные, нужные скорингу совместимости.</summary>
    Task<User?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Пул кандидатов для ленты: активные пользователи, прошедшие <paramref name="filter"/> (пол, радиус от
    /// <see cref="FeedCandidateFilter.OriginCoordinates"/>, возраст, цель знакомств и т.д.), кроме самого
    /// пользователя и уже свайпнутых им (отменённый свайп, T-5.3, возвращает кандидата в пул — фильтр смотрит
    /// на <see cref="Swipe.UndoneAt"/>). Не более <paramref name="poolSize"/>, упорядочены по недавней
    /// активности (её отсутствие — в конец): точный скоринг и сортировка по совместимости — уже в App-слое.
    /// </summary>
    Task<IReadOnlyList<User>> GetCandidatesAsync(
        Guid currentUserId, FeedCandidateFilter filter, int poolSize, CancellationToken cancellationToken);
}
