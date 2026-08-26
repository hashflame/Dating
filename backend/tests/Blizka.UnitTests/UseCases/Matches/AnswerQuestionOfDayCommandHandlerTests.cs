using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Notifications;
using Blizka.App.UseCases.Matches;

namespace Blizka.UnitTests.UseCases.Matches;

public sealed class AnswerQuestionOfDayCommandHandlerTests
{
    [Fact(DisplayName = "КОГДА вопрос дня ещё не публиковался ТОГДА выбрасывается QuestionOfDayNotAvailableException")]
    public async Task Handle_throws_when_no_question_published_yet()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var handler = CreateHandler(match, question: null, out _, out _);

        await Assert.ThrowsAsync<QuestionOfDayNotAvailableException>(
            () => handler.Handle(new AnswerQuestionOfDayCommand(match.Id, currentUser.Id, "Мой ответ"), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА партнёр ещё не отвечал ТОГДА ответ сохраняется, уведомление не отправляется")]
    public async Task Handle_saves_answer_and_skips_notification_when_partner_has_not_answered()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var question = CreateQuestion();
        var handler = CreateHandler(match, question, out var answerRepository, out var notificationService);

        var result = await handler.Handle(
            new AnswerQuestionOfDayCommand(match.Id, currentUser.Id, "Мой ответ"), CancellationToken.None);

        Assert.Equal("Мой ответ", result.Text);
        Assert.True(answerRepository.SaveChangesCalled);
        Assert.Empty(notificationService.Notified);
    }

    [Fact(DisplayName = "КОГДА партнёр уже отвечал ТОГДА мой ответ сохраняется и обоим уходит уведомление")]
    public async Task Handle_notifies_both_participants_once_the_second_one_answers()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var question = CreateQuestion();
        var partnerAnswer = CreateAnswer(question.Id, match.Id, other.Id, "Ответ партнёра");
        var handler = CreateHandler(match, question, out _, out var notificationService, [partnerAnswer]);

        await handler.Handle(new AnswerQuestionOfDayCommand(match.Id, currentUser.Id, "Мой ответ"), CancellationToken.None);

        Assert.Equal(2, notificationService.Notified.Count);
        Assert.Contains(currentUser.Id, notificationService.Notified);
        Assert.Contains(other.Id, notificationService.Notified);
    }

    [Fact(DisplayName = "КОГДА уже отвечал ранее ТОГДА повторный вызов возвращает сохранённый ответ и не перезаписывает его")]
    public async Task Handle_is_idempotent_when_already_answered()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var question = CreateQuestion();
        var existingAnswer = CreateAnswer(question.Id, match.Id, currentUser.Id, "Первый ответ");
        var handler = CreateHandler(match, question, out var answerRepository, out _, [existingAnswer]);

        var result = await handler.Handle(
            new AnswerQuestionOfDayCommand(match.Id, currentUser.Id, "Попытка переписать"), CancellationToken.None);

        Assert.Equal("Первый ответ", result.Text);
        Assert.False(answerRepository.SaveChangesCalled);
    }

    [Fact(DisplayName = "КОГДА текст ответа пустой ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_validation_exception_for_empty_text()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var handler = CreateHandler(match, CreateQuestion(), out _, out _);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => handler.Handle(new AnswerQuestionOfDayCommand(match.Id, currentUser.Id, string.Empty), CancellationToken.None));
    }

    private static AnswerQuestionOfDayCommandHandler CreateHandler(
        Match? match,
        QuestionOfDay? question,
        out FakeQuestionAnswerRepository answerRepository,
        out FakeNotificationService notificationService,
        IReadOnlyList<QuestionAnswer>? existingAnswers = null)
    {
        answerRepository = new FakeQuestionAnswerRepository { Answers = existingAnswers ?? [] };
        notificationService = new FakeNotificationService();

        return new AnswerQuestionOfDayCommandHandler(
            new GetQuestionOfDayQueryHandlerTests.FakeMatchRepository { ById = match },
            new GetQuestionOfDayQueryHandlerTests.FakeQuestionOfDayRepository { Current = question },
            answerRepository,
            new AnswerQuestionOfDayCommandValidator(),
            notificationService);
    }

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

    private sealed class FakeQuestionAnswerRepository : IQuestionAnswerRepository
    {
        public IReadOnlyList<QuestionAnswer> Answers { get; set; } = [];

        public bool SaveChangesCalled { get; private set; }

        public Task<IReadOnlyList<QuestionAnswer>> GetByMatchAndQuestionAsync(
            Guid matchId, Guid questionId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<QuestionAnswer>>(
                Answers.Where(a => a.MatchId == matchId && a.QuestionId == questionId).ToList());

        public Task<IReadOnlyList<QuestionAnswer>> GetByMatchAndQuestionsAsync(
            Guid matchId, IReadOnlyCollection<Guid> questionIds, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ответа на вопрос дня.");

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

    private sealed class FakeNotificationService : INotificationService
    {
        public List<Guid> Notified { get; } = [];

        public Task NotifyMatchAsync(Guid userId, string matchName, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ответа на вопрос дня.");

        public Task NotifyNewProfilesAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ответа на вопрос дня.");

        public Task NotifyCityOpenAsync(IReadOnlyCollection<Guid> userIds, string cityName, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах ответа на вопрос дня.");

        public Task NotifyQuestionOfDayBothAnsweredAsync(Guid userId, CancellationToken cancellationToken)
        {
            Notified.Add(userId);
            return Task.CompletedTask;
        }
    }
}
