using FluentValidation;

namespace Blizka.App.UseCases.Ideas;

public sealed class CreateIdeaCommandValidator : AbstractValidator<CreateIdeaCommand>
{
    // Лимит decomposition.md не задаёт — взят как разумный MVP-предел (тот же порядок, что и User.Bio, T-9.1).
    private const int MaxTextLength = 500;

    public CreateIdeaCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(MaxTextLength);
    }
}
