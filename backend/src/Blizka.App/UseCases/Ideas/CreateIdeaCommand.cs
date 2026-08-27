using Blizka.App.Domain.Enums;
using MediatR;

namespace Blizka.App.UseCases.Ideas;

/// <summary><c>POST /api/ideas</c> — <c>{ text, anonymous }</c> (T-19.1, S-60).</summary>
public sealed record CreateIdeaCommand(Guid UserId, string Text, bool Anonymous) : IRequest<CreateIdeaResult>;

/// <param name="SparksAwarded">
/// Бонус за отправку идеи (<c>Sparks:IdeaSubmissionBonusAmount</c>) — 0, если пользователь уже получал его
/// в этом календарном месяце (по спеке бонус раз в месяц, T-19.1): идея всё равно создаётся, зорки просто не
/// приходят повторно, и об этом сказано в ответе явно, а не молча (тикет ClickUp).
/// </param>
public sealed record CreateIdeaResult(
    Guid Id, string Text, IdeaStatus Status, int VotesCount, bool HasVoted, string? AuthorName, bool IsMine,
    DateTimeOffset CreatedAt, int SparksAwarded);
