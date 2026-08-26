using FluentValidation;

namespace Blizka.App.UseCases.Matches;

public sealed class GetDateIdeasQueryValidator : AbstractValidator<GetDateIdeasQuery>
{
    public GetDateIdeasQueryValidator()
    {
        RuleFor(x => x.MaxBudget).GreaterThan(0).When(x => x.MaxBudget is not null);
        RuleFor(x => x.Currency).Length(3).When(x => !string.IsNullOrEmpty(x.Currency));
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City is not null);
    }
}
