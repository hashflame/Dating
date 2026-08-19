namespace Blizka.App.Domain.Repositories;

public interface ICityRepository
{
    Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken);
}
