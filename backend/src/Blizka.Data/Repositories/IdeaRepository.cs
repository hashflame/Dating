using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Blizka.Data.Repositories;

public sealed class IdeaRepository(BlizkaDbContext dbContext) : IIdeaRepository
{
    public async Task<(IReadOnlyList<IdeaListEntry> Items, int TotalCount)> GetPageAsync(
        IdeaListTab tab, Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = dbContext.Ideas.AsNoTracking().Include(i => i.AuthorUser).AsQueryable();

        query = tab switch
        {
            // «В работе» — underReview+planned (тикет ClickUp: этого сочетания нет в decomposition.md ?sort=hot|new).
            IdeaListTab.InWork => query.Where(i => i.Status == IdeaStatus.UnderReview || i.Status == IdeaStatus.Planned),
            IdeaListTab.Mine => query.Where(i => i.AuthorUserId == currentUserId),
            _ => query,
        };

        query = tab == IdeaListTab.Hot
            ? query.OrderByDescending(i => i.VotesCount).ThenByDescending(i => i.CreatedAt)
            : query.OrderByDescending(i => i.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var ideas = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        if (ideas.Count == 0)
        {
            return ([], totalCount);
        }

        var ideaIds = ideas.Select(i => i.Id).ToList();
        var votedIdeaIds = await dbContext.IdeaVotes
            .Where(v => v.UserId == currentUserId && ideaIds.Contains(v.IdeaId))
            .Select(v => v.IdeaId)
            .ToListAsync(cancellationToken);
        var votedIdeaIdSet = votedIdeaIds.ToHashSet();

        var items = ideas.Select(idea => new IdeaListEntry(idea, votedIdeaIdSet.Contains(idea.Id))).ToList();
        return (items, totalCount);
    }

    public Task<bool> ExistsAsync(Guid ideaId, CancellationToken cancellationToken) =>
        dbContext.Ideas.AsNoTracking().AnyAsync(i => i.Id == ideaId, cancellationToken);

    public async Task AddAsync(Idea idea, CancellationToken cancellationToken) =>
        await dbContext.Ideas.AddAsync(idea, cancellationToken);

    // Самодостаточна: вставка голоса сохраняется сразу же (а не вместе с остальными изменениями запроса),
    // а инкремент Idea.VotesCount — отдельным ExecuteUpdateAsync, атомарным на стороне БД (та же причина, что
    // и MatchRepository.ArchiveStaleMatchesAsync — без него параллельные голоса разных пользователей потеряли
    // бы часть инкрементов при read-modify-write через отслеживаемую сущность).
    public async Task<bool> AddVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken)
    {
        dbContext.IdeaVotes.Add(new IdeaVote { IdeaId = ideaId, UserId = userId, CreatedAt = DateTimeOffset.UtcNow });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateVoteViolation(ex))
        {
            // Уже голосовал (гонка двух почти одновременных POST /vote, либо повторный вызов) — идемпотентно,
            // счётчик не трогаем. Отсоединяем неудавшуюся вставку, как и UserBlockRepository.SaveChangesAsync.
            var conflictingEntry = dbContext.ChangeTracker.Entries<IdeaVote>()
                .FirstOrDefault(entry => entry.State == EntityState.Added);
            if (conflictingEntry is not null)
            {
                conflictingEntry.State = EntityState.Detached;
            }

            return false;
        }

        await dbContext.Ideas
            .Where(i => i.Id == ideaId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.VotesCount, i => i.VotesCount + 1), cancellationToken);

        return true;
    }

    public async Task<bool> RemoveVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken)
    {
        var deleted = await dbContext.IdeaVotes
            .Where(v => v.IdeaId == ideaId && v.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted == 0)
        {
            return false;
        }

        await dbContext.Ideas
            .Where(i => i.Id == ideaId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(i => i.VotesCount, i => i.VotesCount - 1), cancellationToken);

        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private static bool IsDuplicateVoteViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
