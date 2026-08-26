using FluentValidation;

namespace Blizka.App.UseCases.Reports;

public sealed class CreateReportCommandValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportCommandValidator()
    {
        RuleFor(x => x.ReportedUserId)
            .NotEqual(x => x.ReporterUserId)
            .WithMessage("Нельзя пожаловаться на самого себя.");

        RuleFor(x => x.Reason).IsInEnum();

        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}
