namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Telegram Bot API ответил <c>ok: false</c> либо HTTP-ошибкой на вызов <c>ITelegramBotService</c> (T-10.1)
/// после исчерпания всех попыток retry-политики. Не наследует <see cref="BlizkaDomainException"/> — на этом
/// уровне ещё нет клиентского контракта ошибки: как реагировать (повторить позже, залогировать и промолчать,
/// показать пользователю), решает конкретный вызывающий код (T-10.2 уведомления, T-8.2 покупка зорок), а не
/// общий <c>ApiError</c>-каталог.
/// </summary>
public sealed class TelegramApiException(string method, int? telegramErrorCode, string? description, Exception? innerException = null)
    : Exception(
        $"Telegram Bot API method '{method}' failed (errorCode={telegramErrorCode?.ToString() ?? "n/a"}): {description ?? "no description"}",
        innerException)
{
    public string Method { get; } = method;

    public int? TelegramErrorCode { get; } = telegramErrorCode;
}
