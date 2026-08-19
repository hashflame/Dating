using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class UserDatePreferenceRepository(BlizkaDbContext dbContext) : IUserDatePreferenceRepository
{
    public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.UserDatePreferences.CountAsync(p => p.UserId == userId, cancellationToken);
}
