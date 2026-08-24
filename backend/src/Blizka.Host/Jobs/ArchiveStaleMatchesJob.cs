using Blizka.App.Domain.Repositories;
using Quartz;

namespace Blizka.Host.Jobs;

/// <summary>
/// T-7.4 — раз в 6 часов (регистрация триггера в <c>Program.cs</c>) архивирует протухшие мэтчи по условию
/// <see cref="Blizka.App.UseCases.Matches.MatchArchivalPolicy"/>: без открытого контакта дольше 7 дней после
/// мэтча, либо с открытым контактом, но без <c>message-sent-check</c> дольше 7 дней после открытия.
/// </summary>
[DisallowConcurrentExecution]
public sealed class ArchiveStaleMatchesJob(IMatchRepository matchRepository, ILogger<ArchiveStaleMatchesJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var archivedCount = await matchRepository.ArchiveStaleMatchesAsync(DateTimeOffset.UtcNow, context.CancellationToken);

        if (archivedCount > 0)
        {
            logger.LogInformation("ArchiveStaleMatches: заархивировано {ArchivedCount} мэтчей", archivedCount);
        }
    }
}
