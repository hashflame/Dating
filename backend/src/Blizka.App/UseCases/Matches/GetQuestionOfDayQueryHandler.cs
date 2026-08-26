using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.UseCases.Cities;
using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary>Обрабатывает <see cref="GetQuestionOfDayQuery"/> (T-11.1): текущий вопрос дня, мой ответ и ответ партнёра.</summary>
public sealed class GetQuestionOfDayQueryHandler(
    IMatchRepository matchRepository, IQuestionOfDayRepository questionOfDayRepository, IQuestionAnswerRepository questionAnswerRepository)
    : IRequestHandler<GetQuestionOfDayQuery, QuestionOfDayResult>
{
    public async Task<QuestionOfDayResult> Handle(GetQuestionOfDayQuery request, CancellationToken cancellationToken)
    {
        var match = await matchRepository.GetByIdForUserBasicAsync(request.MatchId, request.UserId, cancellationToken)
            ?? throw new MatchNotFoundException(request.MatchId);

        var question = await questionOfDayRepository.GetCurrentAsync(DateTimeOffset.UtcNow, cancellationToken);
        if (question is null)
        {
            return new QuestionOfDayResult(false, null, null, null, null);
        }

        var (me, other) = MatchResultMapper.ResolveUsers(match, request.UserId);
        var locale = CityLocaleResolver.Resolve(me.Locale);

        var answers = await questionAnswerRepository.GetByMatchAndQuestionAsync(request.MatchId, question.Id, cancellationToken);
        var myAnswer = answers.SingleOrDefault(a => a.UserId == request.UserId);
        var partnerAnswer = answers.SingleOrDefault(a => a.UserId == other.Id);
        var bothAnswered = myAnswer is not null && partnerAnswer is not null;

        return new QuestionOfDayResult(
            true,
            question.Id,
            QuestionOfDayTextResolver.Resolve(question, locale),
            myAnswer is null ? null : new QuestionAnswerResult(myAnswer.Text, myAnswer.AnsweredAt),
            bothAnswered ? new QuestionAnswerResult(partnerAnswer!.Text, partnerAnswer.AnsweredAt) : null);
    }
}
