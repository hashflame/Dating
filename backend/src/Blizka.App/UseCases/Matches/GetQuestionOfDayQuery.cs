using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary><c>GET /api/matches/{matchId}/question-of-day</c> (T-11.1).</summary>
public sealed record GetQuestionOfDayQuery(Guid MatchId, Guid UserId) : IRequest<QuestionOfDayResult>;

/// <param name="Available"><c>false</c>, если джоба <c>GenerateQuestionOfDay</c> ещё ни разу не публиковала вопрос — остальные поля тогда <c>null</c>.</param>
/// <param name="MyAnswer">Мой ответ на текущий вопрос, если уже отвечал.</param>
/// <param name="PartnerAnswer">Ответ партнёра — только если ответили оба, иначе <c>null</c> (decomposition.md T-11.1).</param>
public sealed record QuestionOfDayResult(
    bool Available, Guid? QuestionId, string? QuestionText, QuestionAnswerResult? MyAnswer, QuestionAnswerResult? PartnerAnswer);

public sealed record QuestionAnswerResult(string Text, DateTimeOffset AnsweredAt);
