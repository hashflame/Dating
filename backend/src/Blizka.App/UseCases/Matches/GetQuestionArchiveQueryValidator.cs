using FluentValidation;

namespace Blizka.App.UseCases.Matches;

public sealed class GetQuestionArchiveQueryValidator : AbstractValidator<GetQuestionArchiveQuery>
{
    public GetQuestionArchiveQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
