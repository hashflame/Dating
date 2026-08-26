namespace Blizka.Data.Telegram;

/// <summary>Настройки Telegram Bot API (T-10.1) — секция <c>Telegram</c> в appsettings.yaml.</summary>
public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>
    /// Токен бота от BotFather. Пустая строка — валидное значение для локальной разработки (см.
    /// <c>DevLogin:Secret</c> в <c>TelegramAuthMiddleware</c>, обходящий проверку initData) — поэтому здесь
    /// нет <c>ValidateOnStart</c> на непустоту, в отличие от <c>GeoOptions</c>/<c>StorageOptions</c>.
    /// </summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>Секрет для проверки заголовка <c>X-Telegram-Bot-Api-Secret-Token</c> на вебхуке (используется вне этого сервиса).</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Токен платёжного провайдера для <c>createInvoiceLink</c> с фиатной валютой — не нужен при оплате Telegram Stars.</summary>
    public string PaymentProviderToken { get; set; } = string.Empty;
}
