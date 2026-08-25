using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Blizka.Data.Repositories;

public sealed class CityRepository(BlizkaDbContext dbContext) : ICityRepository
{
    // Порог для запросов от 2 букв — ниже дефолтного GUC pg_trgm.similarity_threshold (0.3), иначе короткие
    // запросы, типичные для инкрементального поиска по мере набора текста, не находили бы ничего.
    private const double ShortQuerySimilarityThreshold = 0.15;

    // Для запросов от 4 букв возвращаем дефолтный (более строгий) порог: у 0.15 на длинных запросах
    // многовато ложных срабатываний между городами с общим суффиксом (например, запрос "минск" по нему
    // проходил не только сам "Минск", но и "Пинск"/"Дзержинск"/"Смоленск" — общий трёхграм "нск" на конце).
    private const double LongQuerySimilarityThreshold = 0.3;

    // Порог длины, после которого запрос считается "длинным" (см. LongQuerySimilarityThreshold выше).
    private const int LongQueryMinLength = 4;

    public Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken) =>
        dbContext.Cities.AnyAsync(city => city.Id == cityId, cancellationToken);

    public Task<City?> GetByIdAsync(Guid cityId, CancellationToken cancellationToken) =>
        dbContext.Cities.AsNoTracking().FirstOrDefaultAsync(city => city.Id == cityId, cancellationToken);

    public async Task<IReadOnlyList<City>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken)
    {
        // pg_trgm принципиально не умеет искать по 1 символу — трёхграммы однобуквенного запроса почти не
        // пересекаются с трёхграммами любого настоящего названия города, similarity выходит ниже любого
        // разумного порога (для "м" против "Минск" — около 0.11, ниже даже сниженного ShortQuerySimilarityThreshold).
        // Поэтому для однобуквенного запроса используем обычный prefix-match вместо триграммного сходства.
        if (query.Length == 1)
        {
            return await SearchByPrefixAsync(query, locale, limit, cancellationToken);
        }

        var threshold = query.Length >= LongQueryMinLength ? LongQuerySimilarityThreshold : ShortQuerySimilarityThreshold;

        // Каждая ветка — отдельное выражение с одним и тем же именным столбцом и в Where, и в OrderBy,
        // чтобы Npgsql транслировал вызов EF.Functions.TrigramsSimilarity в SQL (столбец должен быть
        // константой на уровне выражения, а не значением, посчитанным во время выполнения запроса).
        IQueryable<City> matches = locale switch
        {
            CityLocale.Be => dbContext.Cities
                .Where(c => EF.Functions.TrigramsSimilarity(c.NameBe, query) > threshold)
                .OrderByDescending(c => EF.Functions.TrigramsSimilarity(c.NameBe, query)),
            CityLocale.En => dbContext.Cities
                .Where(c => EF.Functions.TrigramsSimilarity(c.NameEn, query) > threshold)
                .OrderByDescending(c => EF.Functions.TrigramsSimilarity(c.NameEn, query)),
            _ => dbContext.Cities
                .Where(c => EF.Functions.TrigramsSimilarity(c.NameRu, query) > threshold)
                .OrderByDescending(c => EF.Functions.TrigramsSimilarity(c.NameRu, query)),
        };

        return await matches.Take(limit).AsNoTracking().ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<City>> SearchByPrefixAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken)
    {
        var pattern = EscapeLikePattern(query) + "%";

        IQueryable<City> matches = locale switch
        {
            CityLocale.Be => dbContext.Cities.Where(c => EF.Functions.ILike(c.NameBe, pattern)).OrderBy(c => c.NameBe),
            CityLocale.En => dbContext.Cities.Where(c => EF.Functions.ILike(c.NameEn, pattern)).OrderBy(c => c.NameEn),
            _ => dbContext.Cities.Where(c => EF.Functions.ILike(c.NameRu, pattern)).OrderBy(c => c.NameRu),
        };

        return await matches.Take(limit).AsNoTracking().ToListAsync(cancellationToken);
    }

    // ILIKE интерпретирует %, _ и \ в самой пользовательской подстроке как спецсимволы шаблона —
    // экранируем их, иначе "%"/"_" в поиске вело бы себя не как буквальный символ, а как wildcard.
    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public async Task<City?> FindNearestAsync(Point location, double maxDistanceMeters, CancellationToken cancellationToken) =>
        await dbContext.Cities
            .Where(c => c.Coordinates.Distance(location) <= maxDistanceMeters)
            .OrderBy(c => c.Coordinates.Distance(location))
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
}
