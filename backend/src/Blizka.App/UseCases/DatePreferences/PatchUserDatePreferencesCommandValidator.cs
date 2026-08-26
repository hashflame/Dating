using FluentValidation;

namespace Blizka.App.UseCases.DatePreferences;

public sealed class PatchUserDatePreferencesCommandValidator : AbstractValidator<PatchUserDatePreferencesCommand>
{
    public PatchUserDatePreferencesCommandValidator()
    {
        RuleForEach(x => x.Codes).IsInEnum();
    }
}
