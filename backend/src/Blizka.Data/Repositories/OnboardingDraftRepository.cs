using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Blizka.Data.Repositories;

public sealed class OnboardingDraftRepository(BlizkaDbContext dbContext) : IOnboardingDraftRepository
{
    public Task<OnboardingDraft?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.OnboardingDrafts.SingleOrDefaultAsync(draft => draft.UserId == userId, cancellationToken);

    public async Task AddAsync(OnboardingDraft draft, CancellationToken cancellationToken) =>
        await dbContext.OnboardingDrafts.AddAsync(draft, cancellationToken);

    public void Remove(OnboardingDraft draft) => dbContext.OnboardingDrafts.Remove(draft);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUserIdPrimaryKeyViolation(ex))
        {
            var conflictingEntry = dbContext.ChangeTracker.Entries<OnboardingDraft>()
                .FirstOrDefault(entry => entry.State == EntityState.Added);

            // Отсоединяем неудавшуюся вставку, иначе последующий GetAsync с тем же UserId не сможет
            // подключить строку конкурента-победителя — в контексте уже числится "Added"-запись с тем же PK.
            var userId = conflictingEntry?.Entity.UserId ?? Guid.Empty;
            if (conflictingEntry is not null)
            {
                conflictingEntry.State = EntityState.Detached;
            }

            throw new ConcurrentOnboardingDraftCreationException(userId, ex);
        }
    }

    private static bool IsUserIdPrimaryKeyViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgresException &&
        postgresException.ConstraintName == "PK_OnboardingDrafts";
}
