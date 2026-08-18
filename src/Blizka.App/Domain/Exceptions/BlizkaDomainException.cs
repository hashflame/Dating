namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Base type for exceptions that represent a known business rule violation and should be
/// translated by the API layer into a structured, localized <c>ApiError</c> response
/// rather than a generic 500.
/// </summary>
public abstract class BlizkaDomainException : Exception
{
    protected BlizkaDomainException(string errorCode, string message, IReadOnlyDictionary<string, object?>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    /// <summary>Machine-readable code (e.g. "INSUFFICIENT_SPARKS") used by the API layer to pick the localized message.</summary>
    public string ErrorCode { get; }

    /// <summary>Structured data for the client (e.g. required/available amounts) — not a display message.</summary>
    public IReadOnlyDictionary<string, object?>? Details { get; }
}
