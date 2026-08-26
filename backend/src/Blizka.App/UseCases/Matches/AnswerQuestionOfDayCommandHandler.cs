using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Repositories;
using Blizka.App.Notifications;
using FluentValidation;
using MediatR;

namespace Blizka.App.UseCases.Matches;

/// <summary>
/// Обрабатывает <see cref="AnswerQuestionOfDayCommand"/> (T-11.1). Идемпотентно: если этот пользователь уже
/// отвечал на текущий вопрос в рамках этого мэтча, повторный вызов просто возвращает сохранённый ответ, не
/// перезаписывая его новым текстом — по аналогии с <c>MessageSentCheckCommandHandler</c>. Когда после сохранения
/// оказывается, что ответили уже оба участника, шлёт обоим Telegram-уведомление (T-10.2).
/// </summary>
public sealed class AnswerQuestionOfDayCommandHandler(
    IMatchRepository matchRepository,
    IQuestionOfDayRepository questionOfDayRepository,
    IQuestionAnswerRepository questionAnswerRepository,
    IValidator<AnswerQuestionOfDayCommand> validator,
    INotificationService? notificationService = null)
    : IRequestHandler<AnswerQuestionOfDayCommand, QuestionAnswerResult>
{
    public async Task<QuestionAnswerResult> Handle(AnswerQuestionOfDayCommand request, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var match = await matchRepository.GetByIdForUserBasicAsync(request.MatchId, request.UserId, cancellationToken)
            ?? throw new MatchNotFoundException(request.MatchId);

        var question = await questionOfDayRepository.GetCurrentAsync(DateTimeOffset.UtcNow, cancellationToken)
            ?? throw new QuestionOfDayNotAvailableException();

        var existingAnswers = await questionAnswerRepository.GetByMatchAndQuestionAsync(request.MatchId, question.Id, cancellationToken);
        var myAnswer = existingAnswers.SingleOrDefault(a => a.UserId == request.UserId);

        if (myAnswer is not null)
        {
            return new QuestionAnswerResult(myAnswer.Text, myAnswer.AnsweredAt);
        }

        var answer = new QuestionAnswer
        {
            Id = Guid.NewGuid(),
            QuestionId = question.Id,
            UserId = request.UserId,
            MatchId = request.MatchId,
            Text = request.Text,
            AnsweredAt = DateTimeOffset.UtcNow,
        };

        await questionAnswerRepository.AddAsync(answer, cancellationToken);

        try
        {
            await questionAnswerRepository.SaveChangesAsync(cancellationToken);
        }
        catch (QuestionAnswerConflictException)
        {
            var current = await questionAnswerRepository.GetByMatchAndQuestionAsync(request.MatchId, question.Id, cancellationToken);
            var alreadySaved = current.Single(a => a.UserId == request.UserId);

            return new QuestionAnswerResult(alreadySaved.Text, alreadySaved.AnsweredAt);
        }

        var (_, other) = MatchResultMapper.ResolveUsers(match, request.UserId);

        // Свежий запрос ПОСЛЕ SaveChangesAsync, а не переиспользование existingAnswers (снапшот до вставки) —
        // если оба партнёра отвечают почти одновременно, снапшот, снятый до своей записи, может не увидеть
        // уже закоммиченный ответ второго, и «оба ответили» не поймает ни один из двух обработчиков. Read
        // Committed гарантирует, что тот, кто сохранился вторым, при повторном чтении увидит уже закоммиченную
        // запись первого — так уведомление отправляется надёжно ровно один раз, тем, кто ответил последним.
        var answersAfterSave = await questionAnswerRepository.GetByMatchAndQuestionAsync(request.MatchId, question.Id, cancellationToken);
        var otherAlreadyAnswered = answersAfterSave.Any(a => a.UserId == other.Id);

        if (otherAlreadyAnswered && notificationService is not null)
        {
            // CancellationToken.None — ответ уже закоммичен, отмена HTTP-запроса клиентом не должна
            // превращать уже успешную операцию в исключение наружу (тот же приём, что в SwipeCommandHandler).
            await notificationService.NotifyQuestionOfDayBothAnsweredAsync(request.UserId, CancellationToken.None);
            await notificationService.NotifyQuestionOfDayBothAnsweredAsync(other.Id, CancellationToken.None);
        }

        return new QuestionAnswerResult(answer.Text, answer.AnsweredAt);
    }
}
