using MediatR;

namespace Blizka.App.UseCases.Notifications;

/// <summary>Счётчик непрочитанного для бейджа уведомлений (T-10.2): <c>GET /api/notifications/unread</c>.</summary>
public sealed record GetUnreadNotificationsCountQuery(Guid UserId) : IRequest<UnreadNotificationsCountResult>;

/// <param name="Likes">
/// Активные входящие лайки/суперлайки без мэтча, появившиеся после <c>User.LastSeenLikesAt</c>
/// (см. <c>ILikesRepository.CountIncomingSinceAsync</c>, T-6.1) — гасится через <c>POST /api/notifications/seen</c>.
/// </param>
/// <param name="Matches">
/// Мэтчи в секции «new» (T-7.1) — <c>Status = Active</c>, контакт ещё не открыт, образовавшиеся после
/// <c>User.LastSeenMatchesAt</c> — гасится тем же <c>POST /api/notifications/seen</c>. Раньше отдельного
/// флага «прочитано» не было и оба счётчика считали вообще все входящие лайки/новые мэтчи — бейдж было
/// невозможно погасить простым просмотром списка (баг из тикета ClickUp).
/// </param>
public sealed record UnreadNotificationsCountResult(int Likes, int Matches);
