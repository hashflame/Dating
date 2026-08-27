using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

public interface IPhotoRepository
{
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Все фото пользователя, отсортированные по <see cref="Photo.SortOrder"/> — единственный способ найти
    /// одно фото по id, поэтому чужие фото не находятся, а не просто скрываются (IDOR-защита).
    /// </summary>
    Task<List<Photo>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(Photo photo, CancellationToken cancellationToken);

    void Remove(Photo photo);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Сохраняет уже применённые к отслеживаемым сущностям изменения (первая фаза), затем вызывает
    /// <paramref name="applySecondPhase"/> и сохраняет ещё раз — обе фазы в одной транзакции БД. Нужен
    /// <c>ReorderPhotosCommandHandler</c> (T-3.1): временные значения первой фазы (отрицательный <c>SortOrder</c>,
    /// <c>IsMain=false</c>) не должны пережить сбой между двумя <see cref="SaveChangesAsync"/> — реализация по
    /// умолчанию (для тестовых фейков, которые её не переопределяют) просто зовёт оба шага без транзакции;
    /// атомарность гарантирует только настоящая EF-реализация в <c>Blizka.Data</c>.
    /// </summary>
    async Task SaveChangesTwoPhaseAsync(Action applySecondPhase, CancellationToken cancellationToken)
    {
        await SaveChangesAsync(cancellationToken);
        applySecondPhase();
        await SaveChangesAsync(cancellationToken);
    }
}
