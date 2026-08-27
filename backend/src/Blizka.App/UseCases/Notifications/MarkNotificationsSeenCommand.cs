using MediatR;

namespace Blizka.App.UseCases.Notifications;

/// <summary>
/// Гасит бейдж(и) непрочитанного (T-10.2, <c>POST /api/notifications/seen</c>) — выставляет
/// <c>User.LastSeenLikesAt</c>/<c>User.LastSeenMatchesAt</c> в текущий момент. Хотя бы один из двух флагов
/// должен быть <c>true</c> (см. <c>MarkNotificationsSeenCommandValidator</c>) — вызов без единого выставленного
/// флага ничего не гасит и вводит в заблуждение.
/// </summary>
public sealed record MarkNotificationsSeenCommand(Guid UserId, bool Likes, bool Matches) : IRequest;
