namespace Blizka.Api.Common;

/// <summary>Структурированный payload ошибки внутри <see cref="ApiErrorResponse"/>.</summary>
/// <param name="Code">Машиночитаемый код ошибки (например, <c>VALIDATION_ERROR</c>).</param>
/// <param name="Message">Локализованное, actionable-сообщение ("что делать"), резолвится на сервере.</param>
/// <param name="Details">Структурированный контекст ошибки (например, требуемая/доступная сумма) или <c>null</c>.</param>
/// <param name="Action">Подсказка клиенту для CTA (например, <c>TOP_UP_SPARKS</c>) или <c>null</c>.</param>
public sealed record ApiError(string Code, string Message, object? Details, string? Action);

/// <summary>Обёртка для каждого неуспешного ответа API.</summary>
/// <param name="Error">Детали ошибки.</param>
public sealed record ApiErrorResponse(ApiError Error)
{
    public static ApiErrorResponse From(string code, string message, object? details = null, string? action = null)
        => new(new ApiError(code, message, details, action));
}
