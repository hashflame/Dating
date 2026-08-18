using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Blizka.Data.Repositories;

public sealed class UserRepository(BlizkaDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.TelegramId == telegramId, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken) =>
        await dbContext.Users.AddAsync(user, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsTelegramIdUniqueViolation(ex))
        {
            var conflictingUser = dbContext.ChangeTracker.Entries<User>()
                .Select(entry => entry.Entity)
                .FirstOrDefault(user => dbContext.Entry(user).State == EntityState.Added);

            throw new ConcurrentUserCreationException(conflictingUser?.TelegramId ?? 0, ex);
        }
    }

    private static bool IsTelegramIdUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException &&
        postgresException.ConstraintName == "IX_Users_TelegramId";
}
