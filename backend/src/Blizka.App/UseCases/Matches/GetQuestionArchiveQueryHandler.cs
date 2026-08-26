using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Cities;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary>Обрабатывает <see cref="GetQuestionArchiveQuery"/> (T-11.1): прошлые вопросы дня, на которые этот мэтч уже отвечал, новые сверху.</summary>
public sealed class GetQuestionArchiveQueryHandler(
    IMatchRepository matchRepository,
    IQuestionOfDayRepository questionOfDayRepository,
    IQuestionAnswerRepository questionAnswerRepository,
    IValidator<GetQuestionArchiveQuery> validator)
    : IRequestHandler<GetQuestionArchiveQuery, QuestionArchiveResult>
{
    public async Task<QuestionArchiveResult> Handle(GetQuestionArchiveQuery request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var match = await matchRepository.GetByIdForUserBasicAsync(request.MatchId, request.UserId, cancellationToken)
            ?? throw new MatchNotFoundException(request.MatchId);

        var (me, other) = MatchResultMapper.ResolveUsers(match, request.UserId);
        var locale = CityLocaleResolver.Resolve(me.Locale);

        var (questions, totalCount) = await questionOfDayRepository.GetArchiveForMatchAsync(
            request.MatchId, request.Page, request.PageSize, cancellationToken);

        var answers = await questionAnswerRepository.GetByMatchAndQuestionsAsync(
            request.MatchId, questions.Select(q => q.Id).ToList(), cancellationToken);

        var items = questions.Select(question =>
        {
            var myAnswer = answers.SingleOrDefault(a => a.QuestionId == question.Id && a.UserId == request.UserId);
            var partnerAnswer = answers.SingleOrDefault(a => a.QuestionId == question.Id && a.UserId == other.Id);
            var bothAnswered = myAnswer is not null && partnerAnswer is not null;

            return new QuestionArchiveItemResult(
                question.Id,
                QuestionOfDayTextResolver.Resolve(question, locale),
                question.PublishedAt,
                myAnswer is null ? null : new QuestionAnswerResult(myAnswer.Text, myAnswer.AnsweredAt),
                bothAnswered ? new QuestionAnswerResult(partnerAnswer!.Text, partnerAnswer.AnsweredAt) : null);
        }).ToList();

        return new QuestionArchiveResult(items, totalCount, request.Page, request.PageSize);
    }
}
