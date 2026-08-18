namespace Blizka.Api.Common;

/// <summary>
/// Structured error payload: <paramref name="Code"/> is machine-readable, <paramref name="Message"/> is
/// localized and actionable ("what to do"), <paramref name="Details"/> carries structured context
/// (e.g. required/available amounts), <paramref name="Action"/> is an optional client-side action hint
/// (e.g. "TOP_UP_SPARKS") the UI can key a CTA off of.
/// </summary>
public sealed record ApiError(string Code, string Message, object? Details, string? Action);

/// <summary>Envelope for every failed API response.</summary>
public sealed record ApiErrorResponse(ApiError Error)
{
    public static ApiErrorResponse From(string code, string message, object? details = null, string? action = null)
        => new(new ApiError(code, message, details, action));
}
