namespace Blizka.App.Notifications;

/// <summary>
/// Уведомление, ожидающее отправки в фоновой очереди (T-10.2). <paramref name="Placeholder"/> — единственная
/// переменная часть шаблона: имя мэтча для <see cref="NotificationType.Match"/>, название города для
/// <see cref="NotificationType.CityOpen"/>, ссылка на архив для <see cref="NotificationType.DataExportReady"/>,
/// <c>null</c> для <see cref="NotificationType.NewProfiles"/> (текст без переменных).
/// </summary>
public sealed record PendingNotification(Guid UserId, NotificationType Type, string? Placeholder);
