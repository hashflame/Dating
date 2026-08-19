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
}
