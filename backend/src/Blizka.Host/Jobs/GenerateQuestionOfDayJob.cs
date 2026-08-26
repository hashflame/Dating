using Blizka.App.Domain.Repositories;
using Quartz;

namespace Blizka.Host.Jobs;

/// <summary>
/// T-11.1 — раз в день в 18:50 (регистрация триггера в <c>Program.cs</c>) выбирает следующий вопрос из каталога
/// (<see cref="IQuestionOfDayRepository.GetNextToPublishAsync"/>). decomposition.md разносит выбор и публикацию
/// по времени (выбрать в 18:50, опубликовать в 19:00) — вместо того чтобы ждать отдельного триггера на 19:00,
/// джоба сразу проставляет <c>PublishedAt</c> на 19:00 того же дня (10-минутный запас на случай, если сама
/// джоба выполняется с задержкой): вопрос физически появляется в каталоге сейчас, но становится «текущим» для
/// <c>GetCurrentAsync</c> (и тем самым видимым в <c>GET /api/matches/{matchId}/question-of-day</c>) только с 19:00.
/// </summary>
[DisallowConcurrentExecution]
public sealed class GenerateQuestionOfDayJob(IQuestionOfDayRepository questionOfDayRepository, ILogger<GenerateQuestionOfDayJob> logger) : IJob
{
    private const int PublishHourUtc = 19;

    public async Task Execute(IJobExecutionContext context)
    {
        var question = await questionOfDayRepository.GetNextToPublishAsync(context.CancellationToken);
        if (question is null)
        {
            // Каталог пуст (сид не применён/удалён вручную) — не должно происходить в норме, но не валим джобу.
            logger.LogWarning("GenerateQuestionOfDay: каталог вопросов дня пуст, публиковать нечего");
            return;
        }

        var now = DateTimeOffset.UtcNow;
        question.PublishedAt = new DateTimeOffset(now.Year, now.Month, now.Day, PublishHourUtc, 0, 0, TimeSpan.Zero);
        await questionOfDayRepository.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation(
            "GenerateQuestionOfDay: вопрос {QuestionId} выбран, публикация в {PublishedAt}", question.Id, question.PublishedAt);
    }
}
