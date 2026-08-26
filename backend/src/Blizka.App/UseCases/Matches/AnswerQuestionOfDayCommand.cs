using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary><c>POST /api/matches/{matchId}/question-of-day/answer</c> (T-11.1).</summary>
public sealed record AnswerQuestionOfDayCommand(Guid MatchId, Guid UserId, string Text) : IRequest<QuestionAnswerResult>;
