using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

/// <summary>Доступ к каталогу вопросов дня (T-11.1).</summary>
public interface IQuestionOfDayRepository
{
    /// <summary>Актуальный на сейчас вопрос — самый свежий по <c>PublishedAt</c> среди уже опубликованных (<c>PublishedAt &lt;= now</c>). <c>null</c>, пока джоба <c>GenerateQuestionOfDay</c> ни разу не отработала.</summary>
    Task<QuestionOfDay?> GetCurrentAsync(DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    /// Следующий вопрос для публикации джобой <c>GenerateQuestionOfDay</c> — сперва ещё ни разу не публиковавшиеся
    /// (<c>PublishedAt IS NULL</c>, по порядку создания), а когда каталог исчерпан — по кругу самый давно
    /// опубликованный. Отслеживается контекстом (для последующего <see cref="SaveChangesAsync"/>).
    /// </summary>
    Task<QuestionOfDay?> GetNextToPublishAsync(CancellationToken cancellationToken);

    /// <summary>Вопросы, на которые матч уже отвечал (хотя бы один участник) — архив (T-11.1), новые сверху.</summary>
    Task<(IReadOnlyList<QuestionOfDay> Questions, int TotalCount)> GetArchiveForMatchAsync(
        Guid matchId, int page, int pageSize, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
