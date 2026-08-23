using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using NetTopologySuite.Geometries;

namespace Blizka.App.Domain.Repositories;

public interface ICityRepository
{
    Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken);

    /// <summary>Город по id, либо <c>null</c>, если такого нет в каталоге — чтобы показать название сохранённого <c>cityId</c> (T-4.1).</summary>
    Task<City?> GetByIdAsync(Guid cityId, CancellationToken cancellationToken);

    /// <summary>Полнотекстовый поиск городов по подстроке через pg_trgm (T-4.1), не более <paramref name="limit"/> результатов.</summary>
    Task<IReadOnlyList<City>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Ближайший к <paramref name="location"/> город каталога в пределах <paramref name="maxDistanceMeters"/>,
    /// либо <c>null</c>, если рядом нет ни одного каталожного города (T-4.1, <c>POST /api/geo/detect</c>).
    /// </summary>
    Task<City?> FindNearestAsync(Point location, double maxDistanceMeters, CancellationToken cancellationToken);
}
