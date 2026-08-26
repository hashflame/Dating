using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Exceptions;
using Blizka.App.UseCases.Matches;

namespace Blizka.UnitTests.UseCases.Matches;

public sealed class GetQuestionArchiveQueryHandlerTests
{
    [Fact(DisplayName = "КОГДА есть прошлые вопросы ТОГДА архив возвращает их с моим и партнёрским ответом и пагинацией")]
    public async Task Handle_returns_archived_questions_with_both_answers_and_paging_metadata()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var question = CreateQuestion();
        var myAnswer = CreateAnswer(question.Id, match.Id, currentUser.Id, "Мой ответ");
        var partnerAnswer = CreateAnswer(question.Id, match.Id, other.Id, "Ответ партнёра");

        var matchRepository = new GetQuestionOfDayQueryHandlerTests.FakeMatchRepository { ById = match };
        var questionOfDayRepository = new GetQuestionOfDayQueryHandlerTests.FakeQuestionOfDayRepository { Archive = [question] };
        var answerRepository = new GetQuestionOfDayQueryHandlerTests.FakeQuestionAnswerRepository { Answers = [myAnswer, partnerAnswer] };
        var handler = new GetQuestionArchiveQueryHandler(
            matchRepository, questionOfDayRepository, answerRepository, new GetQuestionArchiveQueryValidator());

        var result = await handler.Handle(
            new GetQuestionArchiveQuery(match.Id, currentUser.Id, Page: 1, PageSize: 20), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        var item = Assert.Single(result.Items);
        Assert.Equal(question.Id, item.QuestionId);
        Assert.Equal("Мой ответ", item.MyAnswer!.Text);
        Assert.Equal("Ответ партнёра", item.PartnerAnswer!.Text);
    }

    [Fact(DisplayName = "КОГДА мэтча с таким id нет для этого пользователя ТОГДА выбрасывается MatchNotFoundException")]
    public async Task Handle_throws_when_the_match_is_not_found_for_the_requesting_user()
    {
        var handler = new GetQuestionArchiveQueryHandler(
            new GetQuestionOfDayQueryHandlerTests.FakeMatchRepository { ById = null },
            new GetQuestionOfDayQueryHandlerTests.FakeQuestionOfDayRepository(),
            new GetQuestionOfDayQueryHandlerTests.FakeQuestionAnswerRepository(),
            new GetQuestionArchiveQueryValidator());

        await Assert.ThrowsAsync<MatchNotFoundException>(() => handler.Handle(
            new GetQuestionArchiveQuery(Guid.NewGuid(), Guid.NewGuid(), Page: 1, PageSize: 20), CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА page меньше 1 ТОГДА выбрасывается ValidationException")]
    public async Task Handle_throws_validation_exception_for_invalid_page()
    {
        var currentUser = CreateUser();
        var other = CreateUser(name: "Anna");
        var match = CreateMatch(currentUser, other);
        var handler = new GetQuestionArchiveQueryHandler(
            new GetQuestionOfDayQueryHandlerTests.FakeMatchRepository { ById = match },
            new GetQuestionOfDayQueryHandlerTests.FakeQuestionOfDayRepository(),
            new GetQuestionOfDayQueryHandlerTests.FakeQuestionAnswerRepository(),
            new GetQuestionArchiveQueryValidator());

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => handler.Handle(
            new GetQuestionArchiveQuery(match.Id, currentUser.Id, Page: 0, PageSize: 20), CancellationToken.None));
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
        PublishedAt = DateTimeOffset.UtcNow.AddDays(-1),
        CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
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
}
