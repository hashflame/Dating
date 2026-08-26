using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Matches;

namespace Blizka.UnitTests.UseCases.Matches;

public sealed class GetQuestionOfDayQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА вопрос дня ещё не публиковался ТОГДА Available false и остальные поля null")]
    public async Task Handle_returns_unavailable_when_no_question_published_yet()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var handler = CreateHandler(match, question: null);

        var result = await handler.Handle(new GetQuestionOfDayQuery(match.Id, currentUser.Id), CancellationToken.None);

        Assert.False(result.Available);
        Assert.Null(result.QuestionId);
        Assert.Null(result.QuestionText);
        Assert.Null(result.MyAnswer);
        Assert.Null(result.PartnerAnswer);
    }

    [Fact(DisplayName = "КОГДА ответил только партнёр ТОГДА PartnerAnswer скрыт, пока не ответит и текущий пользователь")]
    public async Task Handle_hides_partner_answer_until_i_answer_too()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var question = CreateQuestion();
        var partnerAnswer = CreateAnswer(question.Id, match.Id, other.Id, "Ответ партнёра");
        var handler = CreateHandler(match, question, [partnerAnswer]);

        var result = await handler.Handle(new GetQuestionOfDayQuery(match.Id, currentUser.Id), CancellationToken.None);

        Assert.True(result.Available);
        Assert.Null(result.MyAnswer);
        Assert.Null(result.PartnerAnswer);
    }

    [Fact(DisplayName = "КОГДА ответили оба ТОГДА видны оба ответа")]
    public async Task Handle_reveals_both_answers_once_both_answered()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var question = CreateQuestion();
        var myAnswer = CreateAnswer(question.Id, match.Id, currentUser.Id, "Мой ответ");
        var partnerAnswer = CreateAnswer(question.Id, match.Id, other.Id, "Ответ партнёра");
        var handler = CreateHandler(match, question, [myAnswer, partnerAnswer]);

        var result = await handler.Handle(new GetQuestionOfDayQuery(match.Id, currentUser.Id), CancellationToken.None);

        Assert.Equal("Мой ответ", result.MyAnswer!.Text);
        Assert.Equal("Ответ партнёра", result.PartnerAnswer!.Text);
    }

    [Fact(DisplayName = "КОГДА мэтча с таким id нет для этого пользователя ТОГДА выбрасывается MatchNotFoundException")]
    public async Task Handle_throws_when_the_match_is_not_found_for_the_requesting_user()
    {
        var handler = CreateHandler(match: null, question: null);

        await Assert.ThrowsAsync<MatchNotFoundException>(
            () => handler.Handle(new GetQuestionOfDayQuery(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    private static GetQuestionOfDayQueryHandler CreateHandler(
        Match? match, QuestionOfDay? question, IReadOnlyList<QuestionAnswer>? answers = null) =>
        new(
            new FakeMatchRepository { ById = match },
            new FakeQuestionOfDayRepository { Current = question },
            new FakeQuestionAnswerRepository { Answers = answers ?? [] });

    private static User CreateUser(string name = "Me") => new()
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

    private static Match CreateMatch(User currentUser, User other)
    {
        var (user1, user2) = currentUser.Id.CompareTo(other.Id) < 0 ? (currentUser, other) : (other, currentUser);
        return new Match
        {
            Id = Guid.NewGuid(),
            User1Id = user1.Id,
            User1 = user1,
            User2Id = user2.Id,
            User2 = user2,
            Status = MatchStatus.Active,
            MatchedAt = DateTimeOffset.UtcNow,
        };
    }

    private static QuestionOfDay CreateQuestion() => new()
    {
        Id = Guid.NewGuid(),
        TextRu = "Вопрос",
        TextBe = "Пытанне",
        TextEn = "Question",
        PublishedAt = DateTimeOffset.UtcNow,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static QuestionAnswer CreateAnswer(Guid questionId, Guid matchId, Guid userId, string text) => new()
    {
        Id = Guid.NewGuid(),
        QuestionId = questionId,
        MatchId = matchId,
        UserId = userId,
        Text = text,
        AnsweredAt = DateTimeOffset.UtcNow,
    };

    internal sealed class FakeMatchRepository : IMatchRepository
    {
        public Match? ById { get; set; }

        public Task AddAsync(Match match, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах вопроса дня.");

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах вопроса дня.");

        public void Remove(Match match) => throw new NotSupportedException("Не используется в тестах вопроса дня.");

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах вопроса дня.");

        public Task<int> CountNewAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах вопроса дня.");

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах вопроса дня.");

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах вопроса дня.");

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken)
        {
            var found = ById is not null && ById.Id == matchId && (ById.User1Id == userId || ById.User2Id == userId)
                ? ById
                : null;
            return Task.FromResult(found);
        }

        public Task<Match?> GetByIdForUserBasicAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            GetByIdForUserAsync(matchId, userId, cancellationToken);

        public Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            GetByIdForUserAsync(matchId, userId, cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах вопроса дня.");
    }

    internal sealed class FakeQuestionOfDayRepository : IQuestionOfDayRepository
    {
        public QuestionOfDay? Current { get; set; }

        public IReadOnlyList<QuestionOfDay> Archive { get; set; } = [];

        public Task<QuestionOfDay?> GetCurrentAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            Task.FromResult(Current);

        public Task<QuestionOfDay?> GetNextToPublishAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах вопроса дня.");

        public Task<(IReadOnlyList<QuestionOfDay> Questions, int TotalCount)> GetArchiveForMatchAsync(
            Guid matchId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult((Archive, Archive.Count));

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    internal sealed class FakeQuestionAnswerRepository : IQuestionAnswerRepository
    {
        public IReadOnlyList<QuestionAnswer> Answers { get; set; } = [];

        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<QuestionAnswer>> GetByMatchAndQuestionAsync(
            Guid matchId, Guid questionId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<QuestionAnswer>>(
                Answers.Where(a => a.MatchId == matchId && a.QuestionId == questionId).ToList());

        public Task<IReadOnlyList<QuestionAnswer>> GetByMatchAndQuestionsAsync(
            Guid matchId, IReadOnlyCollection<Guid> questionIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<QuestionAnswer>>(
                Answers.Where(a => a.MatchId == matchId && questionIds.Contains(a.QuestionId)).ToList());

        public Task AddAsync(QuestionAnswer answer, CancellationToken cancellationToken)
        {
            Answers = [.. Answers, answer];
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }
    }
}
