using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Ideas;
using FluentValidation;

namespace Blizka.UnitTests.UseCases.Ideas;

public sealed class GetIdeasQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА идея анонимна ТОГДА authorName null независимо от isMine")]
    public async Task Handle_hides_the_author_name_for_anonymous_ideas()
    {
        var author = Guid.NewGuid();
        var authorUser = CreateUser("Anna");
        var idea = CreateIdea(author, authorUser, isAnonymous: true);
        var repository = new FakeIdeaRepository { Page = ([new IdeaListEntry(idea, HasVoted: false)], 1) };
        var handler = new GetIdeasQueryHandler(repository, new GetIdeasQueryValidator());

        var result = await handler.Handle(new GetIdeasQuery(author, "mine", 1, 20), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Null(item.AuthorName);
        Assert.True(item.IsMine);
    }

    [Fact(DisplayName = "КОГДА идея не анонимна ТОГДА authorName берётся из AuthorUser.Name")]
    public async Task Handle_exposes_the_author_name_for_non_anonymous_ideas()
    {
        var author = Guid.NewGuid();
        var authorUser = CreateUser("Anna");
        var idea = CreateIdea(author, authorUser, isAnonymous: false);
        var repository = new FakeIdeaRepository { Page = ([new IdeaListEntry(idea, HasVoted: true)], 1) };
        var handler = new GetIdeasQueryHandler(repository, new GetIdeasQueryValidator());

        var result = await handler.Handle(new GetIdeasQuery(Guid.NewGuid(), "hot", 1, 20), CancellationToken.None);

        var item = Assert.Single(result.Items);
        Assert.Equal("Anna", item.AuthorName);
        Assert.True(item.HasVoted);
        Assert.False(item.IsMine);
    }

    [Fact(DisplayName = "КОГДА tab не из допустимых значений ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_for_an_unknown_tab()
    {
        var repository = new FakeIdeaRepository();
        var handler = new GetIdeasQueryHandler(repository, new GetIdeasQueryValidator());

        await Assert.ThrowsAsync<ValidationException>(
            () => handler.Handle(new GetIdeasQuery(Guid.NewGuid(), "bogus", 1, 20), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА запрошена страница ТОГДА в репозиторий уходит правильная вкладка")]
    public async Task Handle_passes_the_parsed_tab_to_the_repository()
    {
        var repository = new FakeIdeaRepository();
        var handler = new GetIdeasQueryHandler(repository, new GetIdeasQueryValidator());

        await handler.Handle(new GetIdeasQuery(Guid.NewGuid(), "inWork", 2, 10), CancellationToken.None);

        Assert.Equal(IdeaListTab.InWork, repository.LastTab);
        Assert.Equal(2, repository.LastPage);
        Assert.Equal(10, repository.LastPageSize);
    }

    private static Idea CreateIdea(Guid authorId, User authorUser, bool isAnonymous) => new()
    {
        Id = Guid.NewGuid(),
        AuthorUserId = authorId,
        AuthorUser = authorUser,
        Text = "Add a spectator mode",
        IsAnonymous = isAnonymous,
        Status = IdeaStatus.New,
        VotesCount = 3,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static User CreateUser(string name) => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = name,
        BirthDate = new DateOnly(1995, 1, 1),
        Gender = Gender.Female,
        Locale = "ru",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private sealed class FakeIdeaRepository : IIdeaRepository
    {
        public (IReadOnlyList<IdeaListEntry> Items, int TotalCount) Page { get; set; } = ([], 0);

        public IdeaListTab? LastTab { get; private set; }

        public int? LastPage { get; private set; }

        public int? LastPageSize { get; private set; }

        public Task<(IReadOnlyList<IdeaListEntry> Items, int TotalCount)> GetPageAsync(
            IdeaListTab tab, Guid currentUserId, int page, int pageSize, CancellationToken cancellationToken)
        {
            LastTab = tab;
            LastPage = page;
            LastPageSize = pageSize;
            return Task.FromResult(Page);
        }

        public Task<bool> ExistsAsync(Guid ideaId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах чтения доски идей.");

        public Task AddAsync(Idea idea, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах чтения доски идей.");

        public Task<bool> AddVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах чтения доски идей.");

        public Task<bool> RemoveVoteAsync(Guid ideaId, Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах чтения доски идей.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах чтения доски идей.");
    }
}
