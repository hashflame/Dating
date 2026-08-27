using FluentValidation;

namespace Blizka.App.UseCases.Users;

public sealed class PatchUserProfileCommandValidator : AbstractValidator<PatchUserProfileCommand>
{
    private const int MaxPrompts = 3;
    private const int MaxPromptLength = 200;

    public PatchUserProfileCommandValidator()
    {
        RuleFor(x => x.Name!).NotEmpty().MaximumLength(30).When(x => x.Name is not null);
        RuleFor(x => x.Bio!).MaximumLength(500).When(x => x.Bio is not null);

        // Границы роста decomposition.md не задаёт — взяты как разумный физиологический диапазон (см. T-9.1).
        RuleFor(x => x.Height!.Value).InclusiveBetween(100, 250).When(x => x.Height is not null);

        RuleFor(x => x.Smoking!.Value).IsInEnum().When(x => x.Smoking is not null);
        RuleFor(x => x.Drinking!.Value).IsInEnum().When(x => x.Drinking is not null);
        RuleFor(x => x.Chronotype!.Value).IsInEnum().When(x => x.Chronotype is not null);
        RuleForEach(x => x.DatingGoals!).IsInEnum().When(x => x.DatingGoals is not null);
        // До двух — так в макете S-04 (тикет ClickUp).
        RuleFor(x => x.DatingGoals!)
            .Must(goals => goals.Count <= 2)
            .WithMessage("DatingGoals must contain at most 2 items.")
            .When(x => x.DatingGoals is not null);

        RuleFor(x => x.Prompts!)
            .Must(prompts => prompts.Count <= MaxPrompts)
            .WithMessage($"Prompts must contain at most {MaxPrompts} items.")
            .When(x => x.Prompts is not null);

        RuleForEach(x => x.Prompts!)
            .MaximumLength(MaxPromptLength)
            .When(x => x.Prompts is not null);
    }
}
