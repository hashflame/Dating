using FluentValidation;

namespace Blizka.App.UseCases.Consent;

public sealed class RecordUserConsentCommandValidator : AbstractValidator<RecordUserConsentCommand>
{
    public RecordUserConsentCommandValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Version).NotEmpty().MaximumLength(32);
    }
}
