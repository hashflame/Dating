namespace Blizka.App.Domain.Exceptions;

/// <summary>
/// Базовый тип для исключений, представляющих известное нарушение бизнес-правила; API-слой
/// должен транслировать их в структурированный, локализованный <c>ApiError</c>-ответ,
/// а не в обобщённый 500.
/// </summary>
public abstract class BlizkaDomainException : Exception
{
    protected BlizkaDomainException(string errorCode, string message, IReadOnlyDictionary<string, object?>? details = null)
        : base(message)
    {
        ErrorCode = errorCode;
        Details = details;
    }

    /// <summary>Машиночитаемый код (например, "INSUFFICIENT_SPARKS"), по которому API-слой выбирает локализованное сообщение.</summary>
    public string ErrorCode { get; }

    /// <summary>Структурированные данные для клиента (например, требуемая/доступная сумма) — не текст для показа.</summary>
    public IReadOnlyDictionary<string, object?>? Details { get; }
}
