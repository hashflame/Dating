using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken);

    /// <summary>Загружает пользователя по Id вместе с фото и интересами (нужны для расчёта ProfileCompleteness в T-2.3).</summary>
    Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Загружает пользователя по Id без связанных данных (T-5.2: проверка существования цели свайпа, баланс
    /// зорок; также T-5.4: дефолт ShowGender при создании UserFilter в GetFeedFiltersQueryHandler/PatchFeedFiltersCommandHandler).
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
