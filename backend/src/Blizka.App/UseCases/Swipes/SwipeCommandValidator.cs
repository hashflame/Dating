using FluentValidation;

namespace Blizka.App.UseCases.Swipes;

public sealed class SwipeCommandValidator : AbstractValidator<SwipeCommand>
{
    public SwipeCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();

        RuleFor(x => x.ToUserId)
            .NotEqual(x => x.FromUserId)
            .WithMessage("Нельзя свайпнуть самого себя.");
    }
}
