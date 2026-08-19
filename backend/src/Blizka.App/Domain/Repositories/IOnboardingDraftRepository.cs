using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

public interface IOnboardingDraftRepository
{
    Task<OnboardingDraft?> GetAsync(Guid userId, CancellationToken cancellationToken);

    Task AddAsync(OnboardingDraft draft, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
