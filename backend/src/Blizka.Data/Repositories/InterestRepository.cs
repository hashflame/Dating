using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

/// <summary>По образцу <see cref="CityRepository"/> (T-4.1) — trigram-поиск с теми же порогами похожести.</summary>
public sealed class InterestRepository(BlizkaDbContext dbContext) : IInterestRepository
{
    private const double ShortQuerySimilarityThreshold = 0.15;
    private const double LongQuerySimilarityThreshold = 0.3;
    private const int LongQueryMinLength = 4;

    public async Task<IReadOnlyList<Interest>> GetCatalogAsync(CancellationToken cancellationToken) =>
        await dbContext.Interests.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Interest>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken)
    {
        if (query.Length == 1)
        {
            return await SearchByPrefixAsync(query, locale, limit, cancellationToken);
        }

        var threshold = query.Length >= LongQueryMinLength ? LongQuerySimilarityThreshold : ShortQuerySimilarityThreshold;

        IQueryable<Interest> matches = locale switch
        {
            CityLocale.Be => dbContext.Interests
                .Where(i => EF.Functions.TrigramsSimilarity(i.NameBe, query) > threshold)
                .OrderByDescending(i => EF.Functions.TrigramsSimilarity(i.NameBe, query)),
            CityLocale.En => dbContext.Interests
                .Where(i => EF.Functions.TrigramsSimilarity(i.NameEn, query) > threshold)
                .OrderByDescending(i => EF.Functions.TrigramsSimilarity(i.NameEn, query)),
            _ => dbContext.Interests
                .Where(i => EF.Functions.TrigramsSimilarity(i.NameRu, query) > threshold)
                .OrderByDescending(i => EF.Functions.TrigramsSimilarity(i.NameRu, query)),
        };

        return await matches.Take(limit).AsNoTracking().ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Interest>> SearchByPrefixAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken)
    {
        var pattern = EscapeLikePattern(query) + "%";

        IQueryable<Interest> matches = locale switch
        {
            CityLocale.Be => dbContext.Interests.Where(i => EF.Functions.ILike(i.NameBe, pattern)).OrderBy(i => i.NameBe),
            CityLocale.En => dbContext.Interests.Where(i => EF.Functions.ILike(i.NameEn, pattern)).OrderBy(i => i.NameEn),
            _ => dbContext.Interests.Where(i => EF.Functions.ILike(i.NameRu, pattern)).OrderBy(i => i.NameRu),
        };

        return await matches.Take(limit).AsNoTracking().ToListAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    public async Task<IReadOnlyList<Interest>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await dbContext.Interests.AsNoTracking().Where(i => ids.Contains(i.Id)).ToListAsync(cancellationToken);

    public Task<Interest?> FindByNameAsync(string name, CancellationToken cancellationToken) =>
        dbContext.Interests.AsNoTracking().FirstOrDefaultAsync(i => EF.Functions.ILike(i.NameRu, EscapeLikePattern(name)), cancellationToken);

    public async Task AddAsync(Interest interest, CancellationToken cancellationToken) =>
        await dbContext.Interests.AddAsync(interest, cancellationToken);
}
