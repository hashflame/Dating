using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class FeedRepository(BlizkaDbContext dbContext) : IFeedRepository
{
    public Task<User?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users
            .Include(u => u.City)
            .Include(u => u.UserInterests)
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public async Task<IReadOnlyList<User>> GetCandidatesAsync(
        Guid currentUserId, Guid cityId, Gender preferredGender, int poolSize, CancellationToken cancellationToken)
    {
        var swipedUserIds = dbContext.Swipes
            .Where(s => s.FromUserId == currentUserId && s.UndoneAt == null)
            .Select(s => s.ToUserId);

        return await dbContext.Users
            .Where(u => u.Id != currentUserId)
            .Where(u => u.Status == UserStatus.Active)
            .Where(u => u.CityId == cityId)
            .Where(u => u.Gender == preferredGender)
            .Where(u => !swipedUserIds.Contains(u.Id))
            // Postgres по умолчанию сортирует NULL первыми при DESC — без .HasValue-ключа пользователи без
            // LastActiveAt оказались бы в начале пула вместо конца, вытесняя недавно активных за пределы poolSize.
            .OrderByDescending(u => u.LastActiveAt.HasValue)
            .ThenByDescending(u => u.LastActiveAt)
            .Take(poolSize)
            .Include(u => u.Photos)
            .Include(u => u.UserInterests)
                .ThenInclude(ui => ui.Interest)
            .Include(u => u.City)
            .AsSplitQuery()
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
