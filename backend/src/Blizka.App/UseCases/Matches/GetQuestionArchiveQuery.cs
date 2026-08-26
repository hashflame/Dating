using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary><c>GET /api/matches/{matchId}/questions/archive?page=1</c> (T-11.1).</summary>
public sealed record GetQuestionArchiveQuery(Guid MatchId, Guid UserId, int Page, int PageSize) : IRequest<QuestionArchiveResult>;

public sealed record QuestionArchiveResult(IReadOnlyList<QuestionArchiveItemResult> Items, int TotalCount, int Page, int PageSize);

/// <param name="PartnerAnswer">Ответ партнёра — только если ответили оба (та же логика, что в <see cref="QuestionOfDayResult"/>).</param>
public sealed record QuestionArchiveItemResult(
    Guid QuestionId, string QuestionText, DateTimeOffset? PublishedAt, QuestionAnswerResult? MyAnswer, QuestionAnswerResult? PartnerAnswer);
