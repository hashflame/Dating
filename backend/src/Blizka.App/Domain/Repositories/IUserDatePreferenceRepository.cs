using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

public interface IUserDatePreferenceRepository
{
    /// <summary>Сколько предпочтений по формату свидания выбрал пользователь — используется расчётом ProfileCompleteness (T-2.3).</summary>
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Полный каталог предпочтений по формату свидания (T-9.3) — фиксированные 4 значения из <see cref="Blizka.App.Domain.Enums.DatePreferenceCode"/>.</summary>
    Task<IReadOnlyList<DatePreference>> GetCatalogAsync(CancellationToken cancellationToken);
}
