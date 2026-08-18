namespace Blizka.App.Domain.Exceptions;

/// <summary>Thrown when a spark spend is attempted but the user's balance doesn't cover it.</summary>
public sealed class InsufficientSparksException(int required, int available)
    : BlizkaDomainException(
        "INSUFFICIENT_SPARKS",
        $"Insufficient sparks balance: required {required}, available {available}.",
        new Dictionary<string, object?> { ["required"] = required, ["available"] = available })
{
    public int Required { get; } = required;
    public int Available { get; } = available;
}
