using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

/// <summary>Доступ к персистентным фильтрам ленты (T-5.4).</summary>
public interface IUserFilterRepository
{
    Task<UserFilter?> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(UserFilter filter, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
