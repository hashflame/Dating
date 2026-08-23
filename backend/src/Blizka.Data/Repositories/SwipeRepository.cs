using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Blizka.Data.Repositories;

public sealed class SwipeRepository(BlizkaDbContext dbContext) : ISwipeRepository
{
    private const string SwipeUniqueConstraintName = "IX_Swipes_FromUserId_ToUserId";

    public Task<bool> ExistsActiveAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
        dbContext.Swipes.AnyAsync(
            s => s.FromUserId == fromUserId && s.ToUserId == toUserId && s.UndoneAt == null, cancellationToken);

    public Task<bool> HasActiveMutualLikeAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
        dbContext.Swipes.AnyAsync(
            s => s.FromUserId == toUserId && s.ToUserId == fromUserId && s.UndoneAt == null &&
                (s.Type == SwipeType.Like || s.Type == SwipeType.Superlike),
            cancellationToken);

    public Task<Swipe?> GetLastActiveAsync(Guid fromUserId, CancellationToken cancellationToken) =>
        dbContext.Swipes
            .Where(s => s.FromUserId == fromUserId && s.UndoneAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<int> CountUndoneSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
        dbContext.Swipes.CountAsync(
            s => s.FromUserId == fromUserId && s.UndoneAt != null && s.UndoneAt >= since, cancellationToken);

    public Task<int> CountSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
        dbContext.Swipes.CountAsync(s => s.FromUserId == fromUserId && s.CreatedAt >= since, cancellationToken);

    public Task<DateTimeOffset?> GetOldestCreatedAtSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
        dbContext.Swipes
            .Where(s => s.FromUserId == fromUserId && s.CreatedAt >= since)
            .OrderBy(s => s.CreatedAt)
            .Select(s => (DateTimeOffset?)s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Swipe swipe, CancellationToken cancellationToken) =>
        await dbContext.Swipes.AddAsync(swipe, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsSwipeUniqueViolation(ex))
        {
            var conflictingSwipe = dbContext.ChangeTracker.Entries<Swipe>()
                .Select(entry => entry.Entity)
                .FirstOrDefault(swipe => dbContext.Entry(swipe).State == EntityState.Added);

            throw new ConcurrentSwipeCreationException(
                conflictingSwipe?.FromUserId ?? Guid.Empty, conflictingSwipe?.ToUserId ?? Guid.Empty, ex);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var conflictingUser = dbContext.ChangeTracker.Entries<User>()
                .Select(entry => entry.Entity)
                .FirstOrDefault(user => dbContext.Entry(user).State == EntityState.Modified);

            throw new ConcurrentUserUpdateException(conflictingUser?.Id ?? Guid.Empty, ex);
        }
    }

    private static bool IsSwipeUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException &&
        postgresException.ConstraintName == SwipeUniqueConstraintName;
}
