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

    /// <summary>
    /// Батч-версия <see cref="GetByIdAsync"/> — нужна джобе ShadowbanAutoCheck (T-17.1), чтобы не делать по
    /// отдельному round-trip на каждого кандидата. Реализация по умолчанию (для тестовых фейков, которые её не
    /// переопределяют) просто зовёт <see cref="GetByIdAsync"/> в цикле — эффективна только настоящая EF-реализация
    /// в <c>Blizka.Data</c>, которая делает один запрос <c>WHERE Id IN (...)</c>.
    /// </summary>
    async Task<IReadOnlyList<User>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        var users = new List<User>(ids.Count);
        foreach (var id in ids)
        {
            var user = await GetByIdAsync(id, cancellationToken);
            if (user is not null)
            {
                users.Add(user);
            }
        }

        return users;
    }

    Task AddAsync(User user, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
