using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

public interface IOnboardingDraftRepository
{
    Task<OnboardingDraft?> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(OnboardingDraft draft, CancellationToken cancellationToken);

    void Remove(OnboardingDraft draft);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
