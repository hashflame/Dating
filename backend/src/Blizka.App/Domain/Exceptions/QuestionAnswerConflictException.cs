namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается, когда сохранение ответа на вопрос дня (T-11.1) столкнулось с параллельной вставкой того же
/// ответа (двойное нажатие «Отправить») — по аналогии с <c>ConcurrentSwipeCreationException</c>. Клиенту стоит
/// просто повторить запрос: повторный вызов вернёт уже сохранённый ответ идемпотентно.
/// </summary>
public sealed class QuestionAnswerConflictException(Guid matchId, Exception innerException)
    : BlizkaDomainException(
        "QUESTION_ANSWER_CONFLICT",
        $"Answering the question of the day for match {matchId} conflicted with a concurrent request.",
        new Dictionary<string, object?> { ["matchId"] = matchId },
        innerException);
