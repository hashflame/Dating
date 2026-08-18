namespace Blizka.App.Domain.Exceptions;

/// <summary>Thrown when an action requires a finished onboarding but the user hasn't completed it.</summary>
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
