using Blizka.Api.Common;
using Blizka.App.UseCases.Matches;

namespace Blizka.Api.Matches;

/// <summary>Ответ <c>GET /api/matches/{matchId}/question-of-day</c> (T-11.1).</summary>
public sealed record QuestionOfDayResponse(
    bool Available, Guid? QuestionId, string? QuestionText, QuestionAnswerDto? MyAnswer, QuestionAnswerDto? PartnerAnswer)
{
    public static QuestionOfDayResponse From(QuestionOfDayResult result) => new(
        result.Available,
        result.QuestionId,
        result.QuestionText,
        QuestionAnswerDto.From(result.MyAnswer),
        QuestionAnswerDto.From(result.PartnerAnswer));
}

public sealed record QuestionAnswerDto(string Text, DateTimeOffset AnsweredAt)
{
    public static QuestionAnswerDto? From(QuestionAnswerResult? result) =>
        result is null ? null : new QuestionAnswerDto(result.Text, result.AnsweredAt);
}

/// <summary>Тело <c>POST /api/matches/{matchId}/question-of-day/answer</c>.</summary>
public sealed record AnswerQuestionOfDayRequest(string Text);

/// <summary>Ответ <c>GET /api/matches/{matchId}/questions/archive</c> (T-11.1).</summary>
public sealed record QuestionArchiveItemDto(
    Guid QuestionId, string QuestionText, DateTimeOffset? PublishedAt, QuestionAnswerDto? MyAnswer, QuestionAnswerDto? PartnerAnswer)
{
    public static QuestionArchiveItemDto From(QuestionArchiveItemResult result) => new(
        result.QuestionId,
        result.QuestionText,
        result.PublishedAt,
        QuestionAnswerDto.From(result.MyAnswer),
        QuestionAnswerDto.From(result.PartnerAnswer));
}
