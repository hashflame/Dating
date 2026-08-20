using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;

namespace Blizka.Data.Repositories;

public sealed class MatchRepository(BlizkaDbContext dbContext) : IMatchRepository
{
    public async Task AddAsync(Match match, CancellationToken cancellationToken) =>
        await dbContext.Matches.AddAsync(match, cancellationToken);
}
