using Blizka.App.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Blizka.Data.Repositories;

public sealed class CityRepository(BlizkaDbContext dbContext) : ICityRepository
{
    public Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken) =>
        dbContext.Cities.AnyAsync(city => city.Id == cityId, cancellationToken);
}
