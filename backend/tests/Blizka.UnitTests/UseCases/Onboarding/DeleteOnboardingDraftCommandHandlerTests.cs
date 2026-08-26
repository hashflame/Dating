using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Onboarding;

namespace Blizka.UnitTests.UseCases.Onboarding;

public sealed class DeleteOnboardingDraftCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА у пользователя есть черновик и статус Onboarding ТОГДА черновик удаляется, а статус возвращается в New")]
    public async Task Handle_removes_the_draft_and_resets_the_status()
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 1, Locale = "ru", Status = UserStatus.Onboarding };
        var draft = new OnboardingDraft { UserId = user.Id, Step = 2, DataJson = """{"name":"Ann"}""" };
        var draftRepository = new FakeOnboardingDraftRepository(draft);
        var handler = new DeleteOnboardingDraftCommandHandler(draftRepository, new FakeUserRepository(user), new FakeSwipeRepository());

        await handler.Handle(new DeleteOnboardingDraftCommand(user.Id), CancellationToken.None);

        Assert.Empty(draftRepository.Drafts);
        Assert.Equal(UserStatus.New, user.Status);
    }

    [Fact(DisplayName = "КОГДА у пользователя нет черновика ТОГДА статус всё равно возвращается в New, а вызов не падает")]
    public async Task Handle_resets_the_status_even_when_there_is_no_draft()
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 1, Locale = "ru", Status = UserStatus.Active };
        var handler = new DeleteOnboardingDraftCommandHandler(new FakeOnboardingDraftRepository(), new FakeUserRepository(user), new FakeSwipeRepository());

        await handler.Handle(new DeleteOnboardingDraftCommand(user.Id), CancellationToken.None);

        Assert.Equal(UserStatus.New, user.Status);
    }

    [Fact(DisplayName = "КОГДА пользователь уже New ТОГДА статус не трогается")]
    public async Task Handle_leaves_an_already_new_user_untouched()
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 1, Locale = "ru", Status = UserStatus.New };
        var handler = new DeleteOnboardingDraftCommandHandler(new FakeOnboardingDraftRepository(), new FakeUserRepository(user), new FakeSwipeRepository());

        await handler.Handle(new DeleteOnboardingDraftCommand(user.Id), CancellationToken.None);

        Assert.Equal(UserStatus.New, user.Status);
    }

    [Fact(DisplayName = "КОГДА сохранение сталкивается с конкурентным изменением того же пользователя ТОГДА выбрасывается OnboardingDraftResetConflictException, а не сырой ConcurrentUserUpdateException")]
    public async Task Handle_translates_a_concurrency_conflict_into_a_domain_exception()
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 1, Locale = "ru", Status = UserStatus.Onboarding };
        var handler = new DeleteOnboardingDraftCommandHandler(
            new FakeOnboardingDraftRepository(), new FakeUserRepository(user, simulateConcurrentUpdateConflict: true), new FakeSwipeRepository());

        var exception = await Assert.ThrowsAsync<OnboardingDraftResetConflictException>(
            () => handler.Handle(new DeleteOnboardingDraftCommand(user.Id), CancellationToken.None));

        Assert.Equal(user.Id, exception.UserId);
    }

    [Fact(DisplayName = "КОГДА у пользователя есть собственные свайпы ТОГДА они все удаляются")]
    public async Task Handle_removes_all_swipes_made_by_the_user()
    {
        var user = new User { Id = Guid.NewGuid(), TelegramId = 1, Locale = "ru", Status = UserStatus.Active };
        var swipeRepository = new FakeSwipeRepository();
        var handler = new DeleteOnboardingDraftCommandHandler(
            new FakeOnboardingDraftRepository(), new FakeUserRepository(user), swipeRepository);

        await handler.Handle(new DeleteOnboardingDraftCommand(user.Id), CancellationToken.None);

        Assert.Equal(user.Id, swipeRepository.RemovedForUserId);
    }

    private sealed class FakeOnboardingDraftRepository(params OnboardingDraft[] seed) : IOnboardingDraftRepository
    {
        public List<OnboardingDraft> Drafts { get; } = [.. seed];

        public Task<OnboardingDraft?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Drafts.SingleOrDefault(d => d.UserId == userId));

        public Task AddAsync(OnboardingDraft draft, CancellationToken cancellationToken)
        {
            Drafts.Add(draft);
            return Task.CompletedTask;
        }

        public void Remove(OnboardingDraft draft) => Drafts.Remove(draft);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUserRepository(User user, bool simulateConcurrentUpdateConflict = false) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteOnboardingDraftCommandHandler.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteOnboardingDraftCommandHandler.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id == id ? user : null);

        public Task AddAsync(User newUser, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteOnboardingDraftCommandHandler.");

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (simulateConcurrentUpdateConflict)
            {
                throw new ConcurrentUserUpdateException(user.Id, new InvalidOperationException("simulated xmin conflict"));
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeSwipeRepository : ISwipeRepository
    {
        public Guid? RemovedForUserId { get; private set; }

        public Task<bool> ExistsActiveAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteOnboardingDraftCommandHandler.");

        public Task<bool> HasActiveMutualLikeAsync(Guid fromUserId, Guid toUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteOnboardingDraftCommandHandler.");

        public Task<Swipe?> GetLastActiveAsync(Guid fromUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteOnboardingDraftCommandHandler.");

        public Task<int> CountUndoneSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteOnboardingDraftCommandHandler.");

        public Task<int> CountSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteOnboardingDraftCommandHandler.");

        public Task<DateTimeOffset?> GetOldestCreatedAtSinceAsync(Guid fromUserId, DateTimeOffset since, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteOnboardingDraftCommandHandler.");

        public Task AddAsync(Swipe swipe, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах DeleteOnboardingDraftCommandHandler.");

        public Task RemoveAllInvolvingUserAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("RemoveAllInvolvingUserAsync не используется в этом тесте.");

        public Task RemoveAllByUserAsync(Guid fromUserId, CancellationToken cancellationToken)
        {
            RemovedForUserId = fromUserId;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
