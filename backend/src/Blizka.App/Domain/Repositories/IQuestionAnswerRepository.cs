using Blizka.App.Domain.Entities;

namespace Blizka.App.Domain.Repositories;

/// <summary>Доступ к ответам пар на вопрос дня (T-11.1).</summary>
public interface IQuestionAnswerRepository
{
    /// <summary>Оба ответа (мой и партнёра, если есть) на конкретный вопрос в рамках конкретного мэтча.</summary>
    Task<IReadOnlyList<QuestionAnswer>> GetByMatchAndQuestionAsync(
        Guid matchId, Guid questionId, CancellationToken cancellationToken);

    /// <summary>Пакетная загрузка ответов матча сразу по нескольким вопросам — для страницы архива, чтобы не ходить в БД по одному вопросу за раз.</summary>
    Task<IReadOnlyList<QuestionAnswer>> GetByMatchAndQuestionsAsync(
        Guid matchId, IReadOnlyCollection<Guid> questionIds, CancellationToken cancellationToken);

    Task AddAsync(QuestionAnswer answer, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
