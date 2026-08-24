using FluentValidation;

namespace Blizka.App.UseCases.Sparks;

public sealed class GetSparksWalletQueryValidator : AbstractValidator<GetSparksWalletQuery>
{
    public GetSparksWalletQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 50);
    }
}
