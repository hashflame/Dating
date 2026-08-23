using Blizka.App.Domain.Enums;
using FluentValidation;

namespace Blizka.App.UseCases.Consent;

public sealed class RecordUserConsentCommandValidator : AbstractValidator<RecordUserConsentCommand>
{
    public RecordUserConsentCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Version).NotEmpty().MaximumLength(32);

        // Закон РБ №99-З (spec 002, B4) — совершеннолетие подтверждается явно, отдельно от факта принятия условий.
        RuleFor(x => x.AgeConfirmed)
            .Equal(true)
            .When(x => x.Type == ConsentType.TermsAndPrivacyPolicy)
            .WithMessage("AgeConfirmed is required for TermsAndPrivacyPolicy consent.");
    }
}
