using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Blizka.Data.Repositories;

public sealed class UserRepository(BlizkaDbContext dbContext) : IUserRepository
{
    public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.TelegramId == telegramId, cancellationToken);

    public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Users
            .Include(user => user.Photos)
            .Include(user => user.UserInterests).ThenInclude(ui => ui.Interest)
            .Include(user => user.UserDatePreferences).ThenInclude(p => p.DatePreference)
            .Include(user => user.City)
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken) =>
        await dbContext.Users.Where(user => ids.Contains(user.Id)).ToListAsync(cancellationToken);

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
        catch (DbUpdateException ex) when (IsInterestNameUniqueViolation(ex))
        {
            // Тот же принцип, что и с TelegramId выше — PatchUserInterestsCommandHandler (T-9.2) сохраняет
            // новый кастомный Interest и обновление User в одном SaveChangesAsync, поэтому конфликт по
            // уникальному имени интереса всплывает здесь же.
            var conflictingInterest = dbContext.ChangeTracker.Entries<Interest>()
                .Select(entry => entry.Entity)
                .FirstOrDefault(interest => dbContext.Entry(interest).State == EntityState.Added);

            throw new ConcurrentInterestCreationException(conflictingInterest?.NameRu ?? string.Empty, ex);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var conflictingUser = dbContext.ChangeTracker.Entries<User>()
                .Select(entry => entry.Entity)
                .FirstOrDefault(user => dbContext.Entry(user).State == EntityState.Modified);

            throw new ConcurrentUserUpdateException(conflictingUser?.Id ?? Guid.Empty, ex);
        }
    }

    private static bool IsTelegramIdUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException &&
        postgresException.ConstraintName == "IX_Users_TelegramId";

    private static bool IsInterestNameUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException &&
        postgresException.ConstraintName == "IX_Interests_NameRu_Unique";
}
