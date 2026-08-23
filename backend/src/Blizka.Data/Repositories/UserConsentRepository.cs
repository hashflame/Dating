using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class UserConsentRepository(BlizkaDbContext dbContext) : IUserConsentRepository
{
    public async Task AddAsync(UserConsent consent, CancellationToken cancellationToken) =>
        await dbContext.UserConsents.AddAsync(consent, cancellationToken);

    public Task<bool> HasConsentAsync(Guid userId, ConsentType type, CancellationToken cancellationToken) =>
        dbContext.UserConsents.AnyAsync(c => c.UserId == userId && c.Type == type, cancellationToken);

    public Task<List<UserConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.UserConsents.AsNoTracking().Where(c => c.UserId == userId).ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
