namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается, когда действие требует завершённого онбординга, а пользователь его не прошёл.</summary>
public sealed class OnboardingIncompleteException(string? missingStep = null)
    : BlizkaDomainException(
        "ONBOARDING_INCOMPLETE",
        missingStep is null
            ? "Onboarding is not complete."
            : $"Onboarding is not complete: missing step '{missingStep}'.",
        missingStep is null ? null : new Dictionary<string, object?> { ["missingStep"] = missingStep })
{
    public string? MissingStep { get; } = missingStep;
}
