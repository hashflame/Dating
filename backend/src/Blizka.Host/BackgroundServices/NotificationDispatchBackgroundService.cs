using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Blizka.App.Domain.Services;
using Blizka.App.Notifications;
using Blizka.App.UseCases.Cities;

namespace Blizka.Host.BackgroundServices;

/// <summary>
/// Читает <see cref="INotificationQueue"/> (T-10.2) и шлёт сообщения через <c>ITelegramBotService</c> —
/// один долгоживущий hosted service вместо Quartz-джобы, т.к. очередь событийная (мэтч, открытие города),
/// а не по расписанию. На каждое уведомление открывает свой DI-scope (репозитории и <c>ITelegramBotService</c>
/// — scoped/через <c>AddHttpClient</c>), сбои единичных отправок логируются и не прерывают цикл — иначе одна
/// протухшая запись (пользователь удалён, Telegram отклонил чат) остановила бы всю очередь.
/// </summary>
public sealed class NotificationDispatchBackgroundService(
    INotificationQueue queue, IServiceScopeFactory scopeFactory, ILogger<NotificationDispatchBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await DispatchAsync(notification, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(
                    ex, "Не удалось отправить уведомление {Type} пользователю {UserId}", notification.Type, notification.UserId);
            }
        }
    }

    private async Task DispatchAsync(PendingNotification notification, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var telegramBotService = scope.ServiceProvider.GetRequiredService<ITelegramBotService>();

        var user = await userRepository.GetByIdAsync(notification.UserId, cancellationToken);
        if (user is null || user.Status == UserStatus.Paused)
        {
            return;
        }

        var locale = CityLocaleResolver.Resolve(user.Locale);
        var text = NotificationMessageCatalog.Build(notification.Type, locale, notification.Placeholder);

        await telegramBotService.SendMessageAsync(user.TelegramId, text, TelegramParseMode.None, cancellationToken);
    }
}
