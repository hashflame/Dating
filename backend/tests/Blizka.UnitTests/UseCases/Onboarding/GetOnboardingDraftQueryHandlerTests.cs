using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Onboarding;

namespace Blizka.UnitTests.UseCases.Onboarding;

public sealed class GetOnboardingDraftQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА у пользователя нет черновика ТОГДА возвращается шаг 0 и пустые данные")]
    public async Task Handle_returns_empty_state_when_no_draft_exists()
    {
        var handler = new GetOnboardingDraftQueryHandler(new FakeOnboardingDraftRepository());

        var result = await handler.Handle(new GetOnboardingDraftQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(0, result.Step);
        Assert.Empty(result.Data.EnumerateObject());
    }

    [Fact(DisplayName = "КОГДА у пользователя есть черновик ТОГДА возвращаются его шаг и данные")]
    public async Task Handle_returns_the_stored_draft()
    {
        var userId = Guid.NewGuid();
        var draft = new OnboardingDraft { UserId = userId, Step = 2, DataJson = """{"name":"Ann"}""" };
        var handler = new GetOnboardingDraftQueryHandler(new FakeOnboardingDraftRepository(draft));

        var result = await handler.Handle(new GetOnboardingDraftQuery(userId), CancellationToken.None);

        Assert.Equal(2, result.Step);
        Assert.Equal("Ann", result.Data.GetProperty("name").GetString());
    }

    private sealed class FakeOnboardingDraftRepository(params OnboardingDraft[] seed) : IOnboardingDraftRepository
    {
        private readonly List<OnboardingDraft> _drafts = [.. seed];

        public Task<OnboardingDraft?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(_drafts.SingleOrDefault(d => d.UserId == userId));

        public Task AddAsync(OnboardingDraft draft, CancellationToken cancellationToken)
        {
            _drafts.Add(draft);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
