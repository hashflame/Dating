using FluentValidation;

namespace Blizka.App.UseCases.Matches;

public sealed class AnswerQuestionOfDayCommandValidator : AbstractValidator<AnswerQuestionOfDayCommand>
{
    public AnswerQuestionOfDayCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000);
    }
}
