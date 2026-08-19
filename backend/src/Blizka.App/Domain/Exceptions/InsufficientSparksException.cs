namespace Blizka.App.Domain.Exceptions;

/// <summary>Выбрасывается при попытке потратить зорки, когда баланс пользователя их не покрывает.</summary>
public sealed class InsufficientSparksException(int required, int available)
    : BlizkaDomainException(
        "INSUFFICIENT_SPARKS",
        $"Insufficient sparks balance: required {required}, available {available}.",
        new Dictionary<string, object?> { ["required"] = required, ["available"] = available })
{
    public int Required { get; } = required;
    public int Available { get; } = available;
}
