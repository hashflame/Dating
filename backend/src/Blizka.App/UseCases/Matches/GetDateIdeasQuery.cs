using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary><c>GET /api/matches/{matchId}/date-ideas?city=&amp;maxBudget=&amp;currency=</c> (T-12.1, S-39).</summary>
public sealed record GetDateIdeasQuery(Guid MatchId, Guid UserId, string? City, decimal? MaxBudget, string? Currency) : IRequest<DateIdeasResult>;

public sealed record DateIdeasResult(IReadOnlyList<DateIdeaItemResult> Ideas);

public sealed record DateIdeaItemResult(
    string Title, string Description, decimal EstimatedCost, string Currency, string EstimatedDuration, string InviteText);
