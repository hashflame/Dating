using FluentValidation;

namespace Blizka.App.UseCases.Cities;

public sealed class DetectCityQueryValidator : AbstractValidator<DetectCityQuery>
{
    public DetectCityQueryValidator()
    {
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90);
        RuleFor(x => x.Lon).InclusiveBetween(-180, 180);
    }
}
