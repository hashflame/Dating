using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

public interface IUserRepository
{
    Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken);

    /// <summary>
    /// Загружает пользователя по Id вместе с фото, интересами (с каталожными названиями) и городом — нужны
    /// для расчёта ProfileCompleteness (T-2.3), а также для полного профиля и карточки-превью (T-9.1).
    /// </summary>
    Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Загружает пользователя по Id без связанных данных (T-5.2: проверка существования цели свайпа, баланс
    /// зорок; также T-5.4: дефолт ShowGender при создании UserFilter в GetFeedFiltersQueryHandler/PatchFeedFiltersCommandHandler).
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
