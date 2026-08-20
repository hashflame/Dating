using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

/// <summary>Доступ к данным мэтчей (T-5.2). Сохранение — через <see cref="ISwipeRepository.SaveChangesAsync"/>: свайп, мэтч и списание зорок пишутся одной транзакцией общего DbContext.</summary>
public interface IMatchRepository
{
    Task AddAsync(Match match, CancellationToken cancellationToken);
}
