using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Blizka.Data.Repositories;

public sealed class CityRepository(BlizkaDbContext dbContext) : ICityRepository
{
    // Порог подобрано ниже дефолтного GUC pg_trgm.similarity_threshold (0.3) — иначе короткие запросы
    // (2-3 буквы), типичные для инкрементального поиска по мере набора текста, не находили бы ничего.
    private const double SimilarityThreshold = 0.15;

    public Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken) =>
        dbContext.Cities.AnyAsync(city => city.Id == cityId, cancellationToken);

    public Task<City?> GetByIdAsync(Guid cityId, CancellationToken cancellationToken) =>
        dbContext.Cities.AsNoTracking().FirstOrDefaultAsync(city => city.Id == cityId, cancellationToken);

    public async Task<IReadOnlyList<City>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken)
    {
        // Каждая ветка — отдельное выражение с одним и тем же именным столбцом и в Where, и в OrderBy,
        // чтобы Npgsql транслировал вызов EF.Functions.TrigramsSimilarity в SQL (столбец должен быть
        // константой на уровне выражения, а не значением, посчитанным во время выполнения запроса).
        IQueryable<City> matches = locale switch
        {
            CityLocale.Be => dbContext.Cities
                .Where(c => EF.Functions.TrigramsSimilarity(c.NameBe, query) > SimilarityThreshold)
                .OrderByDescending(c => EF.Functions.TrigramsSimilarity(c.NameBe, query)),
            CityLocale.En => dbContext.Cities
                .Where(c => EF.Functions.TrigramsSimilarity(c.NameEn, query) > SimilarityThreshold)
                .OrderByDescending(c => EF.Functions.TrigramsSimilarity(c.NameEn, query)),
            _ => dbContext.Cities
                .Where(c => EF.Functions.TrigramsSimilarity(c.NameRu, query) > SimilarityThreshold)
                .OrderByDescending(c => EF.Functions.TrigramsSimilarity(c.NameRu, query)),
        };

        return await matches.Take(limit).AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<City?> FindNearestAsync(Point location, double maxDistanceMeters, CancellationToken cancellationToken) =>
        await dbContext.Cities
            .Where(c => c.Coordinates.Distance(location) <= maxDistanceMeters)
            .OrderBy(c => c.Coordinates.Distance(location))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
}
