using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Blizka.Data.Repositories;

public sealed class PrivacySettingsRepository(BlizkaDbContext dbContext) : IPrivacySettingsRepository
{
    public Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.PrivacySettings.AsNoTracking().SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public Task<PrivacySettings?> GetByUserIdTrackedAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.PrivacySettings.SingleOrDefaultAsync(p => p.UserId == userId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, PrivacySettings>> GetByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new Dictionary<Guid, PrivacySettings>();
        }

        var settings = await dbContext.PrivacySettings
            .AsNoTracking()
            .Where(p => userIds.Contains(p.UserId))
            .ToListAsync(cancellationToken);

        return settings.ToDictionary(p => p.UserId);
    }

    public async Task AddAsync(PrivacySettings settings, CancellationToken cancellationToken) =>
        await dbContext.PrivacySettings.AddAsync(settings, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUserIdUniqueViolation(ex))
        {
            var conflictingEntry = dbContext.ChangeTracker.Entries<PrivacySettings>()
                .FirstOrDefault(entry => entry.State == EntityState.Added);

            // Отсоединяем неудавшуюся вставку, иначе последующий GetByUserIdTrackedAsync с тем же UserId не
            // сможет подключить строку конкурента-победителя — в контексте уже числится "Added"-запись.
            var userId = conflictingEntry?.Entity.UserId ?? Guid.Empty;
            if (conflictingEntry is not null)
            {
                conflictingEntry.State = EntityState.Detached;
            }

            throw new ConcurrentPrivacySettingsCreationException(userId, ex);
        }
    }

    private static bool IsUserIdUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException &&
        postgresException.ConstraintName == "IX_PrivacySettings_UserId";
}
