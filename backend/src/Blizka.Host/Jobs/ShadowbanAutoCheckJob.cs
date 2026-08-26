using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Quartz;

namespace Blizka.Host.Jobs;

/// <summary>
/// T-17.1 — раз в 2 часа (регистрация триггера в <c>Program.cs</c>) переводит в <see cref="UserStatus.Shadowbanned"/>
/// пользователей, набравших 3+ жалобы за последние 24 часа. Shadowban скрывает профиль из ленты (T-9.1/T-6.x),
/// но сам пользователь об этом не узнаёт — в отличие от <see cref="UserStatus.Banned"/>, вход в приложение
/// ему не блокируется (нет проверки Status == Shadowbanned в AuthenticateTelegramUserCommandHandler).
/// </summary>
[DisallowConcurrentExecution]
public sealed class ShadowbanAutoCheckJob(
    IReportRepository reportRepository, IUserRepository userRepository, ILogger<ShadowbanAutoCheckJob> logger) : IJob
{
    private const int ReportThresholdCount = 3;
    private static readonly TimeSpan ReportWindow = TimeSpan.FromHours(24);

    public async Task Execute(IJobExecutionContext context)
    {
        var since = DateTimeOffset.UtcNow - ReportWindow;
        var userIds = await reportRepository.GetUsersExceedingReportThresholdAsync(
            since, ReportThresholdCount, context.CancellationToken);

        // Один батч-запрос вместо GetByIdAsync по одному на кандидата (см. GetByIdsAsync — единственная
        // реализация с реальным WHERE Id IN(...), у остальных implementers дефолт из интерфейса).
        var users = await userRepository.GetByIdsAsync(userIds, context.CancellationToken);

        var shadowbannedCount = 0;
        foreach (var user in users)
        {
            // Banned/Deleted уже строже shadowban'а, Shadowbanned повторно ставить незачем — идемпотентность
            // между запусками джобы (жалобы за окно те же, пока не истекут).
            if (user.Status is UserStatus.Banned or UserStatus.Deleted or UserStatus.Shadowbanned)
            {
                continue;
            }

            user.Status = UserStatus.Shadowbanned;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            shadowbannedCount++;
        }

        if (shadowbannedCount > 0)
        {
            await userRepository.SaveChangesAsync(context.CancellationToken);
            logger.LogInformation("ShadowbanAutoCheck: shadowban применён к {ShadowbannedCount} пользователям", shadowbannedCount);
        }
    }
}
