using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class UserDatePreferenceRepository(BlizkaDbContext dbContext) : IUserDatePreferenceRepository
{
    public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.UserDatePreferences.CountAsync(p => p.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<DatePreference>> GetCatalogAsync(CancellationToken cancellationToken) =>
        await dbContext.DatePreferences.AsNoTracking().ToListAsync(cancellationToken);
}
