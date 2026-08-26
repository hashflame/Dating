namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Выбрасывается при попытке ответить на вопрос дня (T-11.1), пока джоба <c>GenerateQuestionOfDay</c> ещё ни разу
/// не опубликовала ни одного вопроса (например, в первый день после деплоя, до 19:00).
/// </summary>
public sealed class QuestionOfDayNotAvailableException()
    : BlizkaDomainException(
        "QUESTION_OF_DAY_NOT_AVAILABLE",
        "No question of the day has been published yet.");
