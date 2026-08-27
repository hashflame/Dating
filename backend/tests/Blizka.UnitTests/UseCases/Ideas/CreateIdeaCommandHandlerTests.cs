using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.Sparks;
using Blizka.App.UseCases.Ideas;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.UseCases.Ideas;

public sealed class CreateIdeaCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА бонус в этом месяце ещё не начислялся ТОГДА идея создаётся и зорки начисляются")]
    public async Task Handle_awards_the_bonus_when_not_yet_awarded_this_month()
    {
        var user = CreateUser();
        var (handler, ideaRepository, sparkTransactionRepository) = CreateHandler(user, awardedThisMonth: false);

        var result = await handler.Handle(new CreateIdeaCommand(user.Id, "Add dark mode", false), CancellationToken.None);

        Assert.Equal(1, result.SparksAwarded);
        Assert.Equal(1, user.SparksBalance);
        Assert.Single(ideaRepository.Added);
        Assert.Equal("Add dark mode", ideaRepository.Added[0].Text);
        Assert.False(ideaRepository.Added[0].IsAnonymous);
        Assert.Equal("User", result.AuthorName);
        Assert.True(result.IsMine);
        Assert.False(result.HasVoted);
        Assert.True(ideaRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА бонус в этом месяце уже начислялся ТОГДА идея всё равно создаётся, но зорки не приходят")]
    public async Task Handle_creates_the_idea_without_a_bonus_when_the_monthly_cap_is_reached()
    {
        var user = CreateUser();
        var (handler, ideaRepository, _) = CreateHandler(user, awardedThisMonth: true);

        var result = await handler.Handle(new CreateIdeaCommand(user.Id, "Add dark mode", false), CancellationToken.None);

        Assert.Equal(0, result.SparksAwarded);
        Assert.Equal(0, user.SparksBalance);
        Assert.Single(ideaRepository.Added);
    }

    [Fact(DisplayName = "КОГДА идея анонимна ТОГДА authorName в ответе null")]
    public async Task Handle_hides_the_author_name_for_an_anonymous_idea()
    {
        var user = CreateUser();
        var (handler, _, _) = CreateHandler(user, awardedThisMonth: false);

        var result = await handler.Handle(new CreateIdeaCommand(user.Id, "Add dark mode", true), CancellationToken.None);

        Assert.Null(result.AuthorName);
    }

    [Fact(DisplayName = "КОГДА текст пустой ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_for_empty_text()
    {
        var user = CreateUser();
        var (handler, _, _) = CreateHandler(user, awardedThisMonth: false);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => handler.Handle(new CreateIdeaCommand(user.Id, "", false), CancellationToken.None));
    }

    private static (CreateIdeaCommandHandler Handler, FakeIdeaRepository IdeaRepository, FakeSparkTransactionRepository SparkTransactionRepository) CreateHandler(
        User user, bool awardedThisMonth)
    {
        var userRepository = new FakeUserRepository([user]);
        var ideaRepository = new FakeIdeaRepository();
        var sparkTransactionRepository = new FakeSparkTransactionRepository { AwardedThisMonth = awardedThisMonth };
        var sparksService = new SparksService(sparkTransactionRepository, userRepository);
        var options = Options.Create(new SparksOptions { IdeaSubmissionBonusAmount = 1 });

        var handler = new CreateIdeaCommandHandler(
            userRepository, ideaRepository, sparkTransactionRepository, sparksService, new CreateIdeaCommandValidator(), options);

        return (handler, ideaRepository, sparkTransactionRepository);
    }

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = "User",
        BirthDate = new DateOnly(1995, 1, 1),
        Gender = Gender.Female,
        Locale = "ru",
        SparksBalance = 0,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeUserRepository(IReadOnlyList<User> users) : IUserRepository
    {
        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах создания идеи.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах создания идеи.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах создания идеи.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeIdeaRepository : IIdeaRepository
    {
        public List<Idea> Added { get; } = [];

        public bool SaveChangesCalled { get; private set; }

        public Task<(IReadOnlyList<IdeaListEntry> Items, int TotalCount)> GetPageAsync(
            IdeaListTab tab, Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах создания идеи.");

        public Task<bool> ExistsAsync(Guid ideaId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах создания идеи.");

        public Task AddAsync(Idea idea, CancellationToken cancellationToken)
        {
            Added.Add(idea);
            return Task.CompletedTask;
        }

        public Task<bool> AddVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах создания идеи.");

        public Task<bool> RemoveVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах создания идеи.");

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSparkTransactionRepository : ISparkTransactionRepository
    {
        public bool AwardedThisMonth { get; set; }

        public List<SparkTransaction> Transactions { get; } = [];

        public Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах создания идеи.");

        public Task<bool> ExistsSinceAsync(Guid userId, SparkTransactionType type, DateTimeOffset since, CancellationToken cancellationToken) =>
            Task.FromResult(AwardedThisMonth);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
