using Blizka.Api.Common;
using Blizka.App.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Blizka.Api.ErrorHandling;

/// <summary>
/// Глобальный маппинг exception → HTTP-статус (T-0.3). Регистрируется через <c>AddExceptionHandler</c> и
/// вызывается middleware'ом фреймворка <c>UseExceptionHandler()</c> в Program.cs.
/// </summary>
public sealed class BlizkaExceptionHandler(ILogger<BlizkaExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, code, action, details) = Classify(exception);
        var locale = RequestLocaleResolver.Resolve(httpContext);
        var message = ErrorMessageCatalog.Resolve(code, locale);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(exception, "Request {Method} {Path} failed with {ErrorCode}",
                httpContext.Request.Method, httpContext.Request.Path, code);
        }

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            ApiErrorResponse.From(code, message, details, action),
            cancellationToken);

        return true;
    }

    private static (int StatusCode, string Code, string? Action, object? Details) Classify(Exception exception) => exception switch
    {
        InsufficientSparksException e => (StatusCodes.Status402PaymentRequired, e.ErrorCode, "TOP_UP_SPARKS", e.Details),
        UserBannedException e => (StatusCodes.Status403Forbidden, e.ErrorCode, "CONTACT_SUPPORT", e.Details),
        UserDeletedException e => (StatusCodes.Status410Gone, e.ErrorCode, null, e.Details),
        OnboardingIncompleteException e => (StatusCodes.Status422UnprocessableEntity, e.ErrorCode, "COMPLETE_ONBOARDING", e.Details),
        OnboardingAlreadyCompletedException e => (StatusCodes.Status409Conflict, e.ErrorCode, null, e.Details),
        CityNotOpenException e => (StatusCodes.Status409Conflict, e.ErrorCode, "JOIN_CITY_WAITLIST", e.Details),
        PhotoLimitExceededException e => (StatusCodes.Status422UnprocessableEntity, e.ErrorCode, "DELETE_A_PHOTO", e.Details),
        PhotoNotFoundException e => (StatusCodes.Status404NotFound, e.ErrorCode, null, e.Details),
        PhotoUploadConflictException e => (StatusCodes.Status409Conflict, e.ErrorCode, "RETRY_UPLOAD", e.Details),
        AlreadySwipedException e => (StatusCodes.Status409Conflict, e.ErrorCode, null, e.Details),
        SwipeTargetNotFoundException e => (StatusCodes.Status404NotFound, e.ErrorCode, null, e.Details),
        SwipeConflictException e => (StatusCodes.Status409Conflict, e.ErrorCode, "RETRY", e.Details),
        ValidationException e => (StatusCodes.Status400BadRequest, ErrorMessageCatalog.ValidationError, null, BuildValidationDetails(e)),
        _ => (StatusCodes.Status500InternalServerError, ErrorMessageCatalog.InternalError, null, null),
    };

    private static IReadOnlyDictionary<string, string[]> BuildValidationDetails(ValidationException exception) =>
        exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
