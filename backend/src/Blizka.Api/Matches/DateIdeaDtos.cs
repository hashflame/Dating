using Blizka.App.UseCases.Matches;

namespace Blizka.Api.Matches;

/// <summary>Ответ <c>GET /api/matches/{matchId}/date-ideas</c> (T-12.1, S-39) — MVP-заглушка: подбор из фиксированного каталога, не LLM-генерация (T-13.1 не реализована).</summary>
public sealed record DateIdeasResponse(DateIdeaDto[] Ideas)
{
    public static DateIdeasResponse From(DateIdeasResult result) => new(result.Ideas.Select(DateIdeaDto.From).ToArray());
}

public sealed record DateIdeaDto(
    string Title, string Description, decimal EstimatedCost, string Currency, string EstimatedDuration, string InviteText)
{
    public static DateIdeaDto From(DateIdeaItemResult result) => new(
        result.Title, result.Description, result.EstimatedCost, result.Currency, result.EstimatedDuration, result.InviteText);
}
