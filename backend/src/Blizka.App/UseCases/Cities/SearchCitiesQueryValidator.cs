using FluentValidation;

namespace Blizka.App.UseCases.Cities;

public sealed class SearchCitiesQueryValidator : AbstractValidator<SearchCitiesQuery>
{
    public SearchCitiesQueryValidator()
    {
        RuleFor(x => x.Q).NotEmpty().MaximumLength(100);
    }
}
