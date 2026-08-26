using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Blizka.Data.Repositories;

public sealed class UserBlockRepository(BlizkaDbContext dbContext) : IUserBlockRepository
{
    public Task<bool> ExistsAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
        dbContext.UserBlocks.AsNoTracking()
            .AnyAsync(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId, cancellationToken);

    public Task<bool> ExistsEitherDirectionAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken) =>
        dbContext.UserBlocks.AsNoTracking()
            .AnyAsync(
                b => (b.BlockerUserId == userId && b.BlockedUserId == otherUserId)
                    || (b.BlockerUserId == otherUserId && b.BlockedUserId == userId),
                cancellationToken);

    public async Task<IReadOnlyList<UserBlock>> GetBlockedByUserAsync(Guid blockerUserId, CancellationToken cancellationToken) =>
        await dbContext.UserBlocks.AsNoTracking()
            .Where(b => b.BlockerUserId == blockerUserId)
            .Include(b => b.BlockedUser)
                .ThenInclude(u => u!.Photos)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserBlock block, CancellationToken cancellationToken) =>
        await dbContext.UserBlocks.AddAsync(block, cancellationToken);

    // ExecuteDeleteAsync, а не Remove+SaveChanges — разблокировка идемпотентна по своей природе (0 удалённых
    // строк, если блокировки уже не было, не считается ошибкой), отдельная транзакция ей не нужна.
    public async Task RemoveAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
        await dbContext.UserBlocks
            .Where(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId)
            .ExecuteDeleteAsync(cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsDuplicateBlockViolation(ex))
        {
            // Гонка двух почти одновременных POST /block от одного пользователя — сама блокировка уже стоит
            // (кто-то её выиграл), поэтому это не ошибка, а тот же результат, к которому шёл текущий запрос.
            // Отсоединяем неудавшуюся вставку, иначе она останется висеть в ChangeTracker как "Added".
            var conflictingEntry = dbContext.ChangeTracker.Entries<UserBlock>()
                .FirstOrDefault(entry => entry.State == EntityState.Added);
            if (conflictingEntry is not null)
            {
                conflictingEntry.State = EntityState.Detached;
            }
        }
    }

    private static bool IsDuplicateBlockViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException &&
        postgresException.ConstraintName == "IX_UserBlocks_BlockerUserId_BlockedUserId";
}
