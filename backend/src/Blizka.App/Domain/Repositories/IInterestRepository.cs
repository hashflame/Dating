using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;

namespace Blizka.App.Domain.Repositories;

public interface IInterestRepository
{
    /// <summary>Полный каталог интересов (T-9.2), включая ранее созданные пользователями кастомные — общий для всех.</summary>
    Task<IReadOnlyList<Interest>> GetCatalogAsync(CancellationToken cancellationToken);

    /// <summary>Полнотекстовый поиск по каталогу по подстроке через pg_trgm (T-9.2), по образцу <see cref="ICityRepository.SearchAsync"/>.</summary>
    Task<IReadOnlyList<Interest>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<Interest>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    /// <summary>Ищет уже существующий (в том числе кастомный) интерес по точному совпадению названия без учёта регистра — чтобы не плодить дубликаты custom-интересов.</summary>
    Task<Interest?> FindByNameAsync(string name, CancellationToken cancellationToken);

    Task AddAsync(Interest interest, CancellationToken cancellationToken);
}
