using Blizka.App.Domain.Repositories;
using FluentValidation;

namespace Blizka.App.UseCases.Onboarding;

public sealed class OnboardingStep1DataValidator : AbstractValidator<OnboardingStep1Data>
{
    public OnboardingStep1DataValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Gender).IsInEnum();
        RuleFor(x => x.BirthDate)
            .Must(BeAtLeast18YearsOld)
            .WithMessage("BirthDate must correspond to an age of at least 18.");
    }

    private static bool BeAtLeast18YearsOld(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Year;
        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        return age >= 18;
    }
}

public sealed class OnboardingStep2DataValidator : AbstractValidator<OnboardingStep2Data>
{
    public OnboardingStep2DataValidator()
    {
        RuleFor(x => x.ShowGender).IsInEnum();
        RuleFor(x => x.DatingGoals).NotEmpty();
        RuleForEach(x => x.DatingGoals).IsInEnum();

        RuleFor(x => x.AgeRange).NotNull();
        RuleFor(x => x.AgeRange)
            .Must(range => range.Min < range.Max)
            .WithMessage("AgeRange.Min must be less than AgeRange.Max.")
            .When(x => x.AgeRange is not null);
    }
}

public sealed class OnboardingStep3DataValidator : AbstractValidator<OnboardingStep3Data>
{
    public OnboardingStep3DataValidator(ICityRepository cityRepository)
    {
        RuleFor(x => x.CityId)
            .MustAsync((cityId, cancellationToken) => cityRepository.ExistsAsync(cityId, cancellationToken))
            .WithMessage("CityId does not reference an existing city.");

        RuleFor(x => x.Coordinates!.Lat)
            .InclusiveBetween(-90, 90)
            .When(x => x.Coordinates is not null)
            .WithMessage("Coordinates.Lat must be between -90 and 90.");

        RuleFor(x => x.Coordinates!.Lng)
            .InclusiveBetween(-180, 180)
            .When(x => x.Coordinates is not null)
            .WithMessage("Coordinates.Lng must be between -180 and 180.");
    }
}
