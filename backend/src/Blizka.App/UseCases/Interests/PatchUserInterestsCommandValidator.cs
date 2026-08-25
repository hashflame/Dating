using FluentValidation;

namespace Blizka.App.UseCases.Interests;

public sealed class PatchUserInterestsCommandValidator : AbstractValidator<PatchUserInterestsCommand>
{
    // Лимит decomposition.md не задаёт — взят как разумное ограничение против злоупотребления
    // созданием кастомных интересов (см. InterestLimitExceededException, T-9.2).
    private const int MaxInterests = 20;
    private const int MaxCustomInterestNameLength = 50;

    public PatchUserInterestsCommandValidator()
    {
        RuleForEach(x => x.InterestIds).NotEqual(Guid.Empty);

        RuleForEach(x => x.CustomInterests).NotEmpty().MaximumLength(MaxCustomInterestNameLength);

        RuleFor(x => x)
            .Must(x => x.InterestIds.Count + x.CustomInterests.Count <= MaxInterests)
            .WithMessage($"Interests must contain at most {MaxInterests} items in total.");
    }
}
