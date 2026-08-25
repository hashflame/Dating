using FluentValidation;

namespace Blizka.App.UseCases.Interests;

/// <summary>По образцу <see cref="Cities.SearchCitiesQueryValidator"/> (T-4.1).</summary>
public sealed class SearchInterestsQueryValidator : AbstractValidator<SearchInterestsQuery>
{
    public SearchInterestsQueryValidator()
    {
        RuleFor(x => x.Q).NotEmpty().MaximumLength(50);
    }
}
