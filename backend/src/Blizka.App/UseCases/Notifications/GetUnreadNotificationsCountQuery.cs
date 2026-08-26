using MediatR;

namespace Blizka.App.UseCases.Notifications;

/// <summary>Счётчик непрочитанного для бейджа уведомлений (T-10.2): <c>GET /api/notifications/unread</c>.</summary>
public sealed record GetUnreadNotificationsCountQuery(Guid UserId) : IRequest<UnreadNotificationsCountResult>;

/// <param name="Likes">Активные входящие лайки/суперлайки без мэтча (см. <c>ILikesRepository.CountIncomingAsync</c>, T-6.1).</param>
/// <param name="Matches">
/// Мэтчи в секции «new» (T-7.1) — <c>Status = Active</c>, контакт ещё не открыт. Отдельного флага
/// «прочитано»/таймстампа последнего просмотра уведомлений в домене нет (decomposition.md не задаёт его для
/// T-10.2) — открытие контакта уже само по себе снимает мэтч из «new», так что это естественная граница
/// «непрочитанного» без новой сущности.
/// </param>
public sealed record UnreadNotificationsCountResult(int Likes, int Matches);
