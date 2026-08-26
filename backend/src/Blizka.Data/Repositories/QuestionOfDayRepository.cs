using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class QuestionOfDayRepository(BlizkaDbContext dbContext) : IQuestionOfDayRepository
{
    public Task<QuestionOfDay?> GetCurrentAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.QuestionsOfDay
            .AsNoTracking()
            .Where(q => q.PublishedAt != null && q.PublishedAt <= now)
            .OrderByDescending(q => q.PublishedAt)
            .FirstOrDefaultAsync(cancellationToken);

    // Ещё не опубликованные (PublishedAt IS NULL) идут раньше уже опубликованных — тай-брейкер CreatedAt
    // внутри каждой из групп: непубликовавшиеся уходят по порядку создания (сид-каталога), а когда каталог
    // исчерпан — по кругу перепубликуется самый давно публиковавшийся.
    public Task<QuestionOfDay?> GetNextToPublishAsync(CancellationToken cancellationToken) =>
        dbContext.QuestionsOfDay
            .OrderBy(q => q.PublishedAt == null ? 0 : 1)
            .ThenBy(q => q.PublishedAt)
            .ThenBy(q => q.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<(IReadOnlyList<QuestionOfDay> Questions, int TotalCount)> GetArchiveForMatchAsync(
        Guid matchId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.QuestionsOfDay
            .AsNoTracking()
            .Where(q => dbContext.QuestionAnswers.Any(a => a.QuestionId == q.Id && a.MatchId == matchId))
            .OrderByDescending(q => q.PublishedAt)
            .ThenByDescending(q => q.Id);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);
}
