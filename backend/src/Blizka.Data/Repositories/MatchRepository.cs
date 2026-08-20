using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class MatchRepository(BlizkaDbContext dbContext) : IMatchRepository
{
    public async Task AddAsync(Match match, CancellationToken cancellationToken) =>
        await dbContext.Matches.AddAsync(match, cancellationToken);

    public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken)
    {
        var (user1Id, user2Id) = userId1.CompareTo(userId2) < 0 ? (userId1, userId2) : (userId2, userId1);

        return dbContext.Matches.SingleOrDefaultAsync(
            m => m.User1Id == user1Id && m.User2Id == user2Id, cancellationToken);
    }

    public void Remove(Match match) => dbContext.Matches.Remove(match);
}
