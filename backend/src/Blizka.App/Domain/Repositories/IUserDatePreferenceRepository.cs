namespace Blizka.App.Domain.Repositories;

public interface IUserDatePreferenceRepository
{
    /// <summary>Сколько предпочтений по формату свидания выбрал пользователь — используется расчётом ProfileCompleteness (T-2.3).</summary>
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}
