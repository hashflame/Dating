using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Blizka.Data.Repositories;

public sealed class UserFilterRepository(BlizkaDbContext dbContext) : IUserFilterRepository
{
    public Task<UserFilter?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.UserFilters.SingleOrDefaultAsync(f => f.UserId == userId, cancellationToken);

    public async Task AddAsync(UserFilter filter, CancellationToken cancellationToken) =>
        await dbContext.UserFilters.AddAsync(filter, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUserIdPrimaryKeyViolation(ex))
        {
            var conflictingEntry = dbContext.ChangeTracker.Entries<UserFilter>()
                .FirstOrDefault(entry => entry.State == EntityState.Added);

            // Отсоединяем неудавшуюся вставку, иначе последующий GetAsync с тем же UserId не сможет
            // подключить строку конкурента-победителя — в контексте уже числится "Added"-запись с тем же PK.
            var userId = conflictingEntry?.Entity.UserId ?? Guid.Empty;
            if (conflictingEntry is not null)
            {
                conflictingEntry.State = EntityState.Detached;
            }

            throw new ConcurrentUserFilterCreationException(userId, ex);
        }
    }

    private static bool IsUserIdPrimaryKeyViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException &&
        postgresException.ConstraintName == "PK_UserFilters";
}
