namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается, когда идеи с таким id нет (T-19.1) — например, при <c>POST /api/ideas/{ideaId}/vote</c>.</summary>
public sealed class IdeaNotFoundException(Guid ideaId)
    : BlizkaDomainException(
        "IDEA_NOT_FOUND",
        $"Idea {ideaId} was not found.",
        new Dictionary<string, object?> { ["ideaId"] = ideaId })
{
    public Guid IdeaId { get; } = ideaId;
}
