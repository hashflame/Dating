using FluentValidation;

namespace Blizka.App.UseCases.Ideas;

public sealed class GetIdeasQueryValidator : AbstractValidator<GetIdeasQuery>
{
    public GetIdeasQueryValidator()
    {
        RuleFor(x => x.Tab).Must(IdeaListTabParser.AllowedValues.Contains)
            .WithMessage($"Tab must be one of: {string.Join(", ", IdeaListTabParser.AllowedValues)}.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
