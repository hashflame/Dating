using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class MatchRepository(BlizkaDbContext dbContext) : IMatchRepository
{
    public async Task AddAsync(Match match, CancellationToken cancellationToken) =>
        await dbContext.Matches.AddAsync(match, cancellationToken);

    public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken)
    {
        var (user1Id, user2Id) = userId1.CompareTo(userId2) < 0 ? (userId1, userId2) : (userId2, userId1);

        return dbContext.Matches.SingleOrDefaultAsync(
            m => m.User1Id == user1Id && m.User2Id == user2Id, cancellationToken);
    }

    public void Remove(Match match) => dbContext.Matches.Remove(match);

    public async Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
        await WithUsers(ForUser(userId))
            .Where(m => m.Status == MatchStatus.Active && m.ContactUnlockedAt == null)
            .OrderByDescending(m => m.MatchedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
        await WithUsers(ForUser(userId))
            .Where(m => m.Status == MatchStatus.Active && m.ContactUnlockedAt != null && m.MessageSentCheckAt == null)
            .OrderByDescending(m => m.ContactUnlockedAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
        await WithUsers(ForUser(userId))
            .Where(m => m.Status == MatchStatus.Archived)
            .OrderByDescending(m => m.ArchivedAt ?? m.MatchedAt)
            .ToListAsync(cancellationToken);

    public async Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
        await WithUsers(ForUser(userId))
            .SingleOrDefaultAsync(m => m.Id == matchId, cancellationToken);

    private IQueryable<Match> ForUser(Guid userId) =>
        dbContext.Matches.AsNoTracking().Where(m => m.User1Id == userId || m.User2Id == userId);

    // Обе стороны мэтча грузятся целиком (фото, интересы+Interest, город) — вторая сторона идёт в проекцию
    // MatchUserResult, а своя сторона (кто из User1/User2 совпал с userId) нужна для FeedCompatibilityScorer
    // при подсчёте бейджа fire в секции new. AsSplitQuery — по тому же соображению, что и в FeedRepository:
    // две коллекции (Photos, UserInterests) на двух связанных сущностях иначе дали бы декартово произведение.
    private static IQueryable<Match> WithUsers(IQueryable<Match> query) =>
        query
            .Include(m => m.User1!).ThenInclude(u => u!.Photos)
            .Include(m => m.User1!).ThenInclude(u => u!.UserInterests).ThenInclude(ui => ui.Interest)
            .Include(m => m.User1!).ThenInclude(u => u!.City)
            .Include(m => m.User2!).ThenInclude(u => u!.Photos)
            .Include(m => m.User2!).ThenInclude(u => u!.UserInterests).ThenInclude(ui => ui.Interest)
            .Include(m => m.User2!).ThenInclude(u => u!.City)
            .AsSplitQuery();
}
