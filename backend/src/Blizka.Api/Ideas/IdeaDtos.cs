using Blizka.App.Domain.Enums;
using Blizka.App.UseCases.Ideas;

namespace Blizka.Api.Ideas;

/// <summary>Элемент доски идей (T-19.1, S-60).</summary>
/// <param name="Status">camelCase-строка: <c>new | underReview | planned | implemented | declined</c>.</param>
/// <param name="AuthorName"><c>null</c>, если автор отправил идею анонимно — независимо от <paramref name="IsMine"/>.</param>
public sealed record IdeaDto(
    Guid Id, string Text, string Status, int VotesCount, bool HasVoted, string? AuthorName, bool IsMine, DateTimeOffset CreatedAt)
{
    public static IdeaDto From(IdeaItemResult result) => new(
        result.Id, result.Text, StatusToString(result.Status), result.VotesCount, result.HasVoted,
        result.AuthorName, result.IsMine, result.CreatedAt);

    internal static string StatusToString(IdeaStatus status) => status switch
    {
        IdeaStatus.New => "new",
        IdeaStatus.UnderReview => "underReview",
        IdeaStatus.Planned => "planned",
        IdeaStatus.Implemented => "implemented",
        IdeaStatus.Declined => "declined",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}

/// <summary>Тело <c>POST /api/ideas</c> (T-19.1).</summary>
public sealed record CreateIdeaRequest(string Text, bool Anonymous);

/// <param name="SparksAwarded">Начислена ли зорка за эту идею — 0, если месячный лимит бонуса уже исчерпан (идея всё равно принимается).</param>
public sealed record CreateIdeaResponse(
    Guid Id, string Text, string Status, int VotesCount, bool HasVoted, string? AuthorName, bool IsMine,
    DateTimeOffset CreatedAt, int SparksAwarded)
{
    public static CreateIdeaResponse From(CreateIdeaResult result) => new(
        result.Id, result.Text, IdeaDto.StatusToString(result.Status), result.VotesCount, result.HasVoted,
        result.AuthorName, result.IsMine, result.CreatedAt, result.SparksAwarded);
}
