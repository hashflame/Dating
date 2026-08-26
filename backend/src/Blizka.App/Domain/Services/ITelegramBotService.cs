namespace Blizka.App.Domain.Services;

/// <summary>
/// Инфраструктурный клиент Telegram Bot API (T-10.1) — отправка уведомлений, создание ссылок на оплату
/// (зорки, подписка) и импорт фото профиля пользователя из Telegram. Единая точка входа в Bot API для всего
/// бэкенда: реализация несёт retry-политику и ограничение частоты запросов, вызывающему коду об этом думать
/// не нужно.
/// </summary>
public interface ITelegramBotService
{
    /// <summary>Отправляет текстовое сообщение пользователю в личный чат с ботом.</summary>
    Task SendMessageAsync(long telegramId, string text, TelegramParseMode parseMode, CancellationToken cancellationToken);

    /// <summary>Создаёт одноразовую ссылку на оплату (invoice) — для покупки зорок (T-8.2) или подписки (T-8.3).</summary>
    Task<string> CreateInvoiceLinkAsync(TelegramInvoice invoice, CancellationToken cancellationToken);

    /// <summary>
    /// Возвращает публичные ссылки на фото профиля пользователя в Telegram (для импорта аватара, T-3.1) —
    /// от самого свежего к более старым, не более <paramref name="limit"/> штук.
    /// </summary>
    Task<TelegramUserProfilePhotos> GetUserProfilePhotosAsync(long telegramId, int limit, CancellationToken cancellationToken);
}

/// <summary>Режим форматирования текста сообщения — соответствует значениям <c>parse_mode</c> Telegram Bot API.</summary>
public enum TelegramParseMode
{
    /// <summary>Без форматирования — обычный текст.</summary>
    None,
    MarkdownV2,
    Html,
}

/// <param name="Payload">Непрозрачная строка, которую Telegram вернёт назад в апдейте <c>successful_payment</c> — сюда кладётся то, что нужно, чтобы на бэкенде сопоставить оплату с покупкой (например, userId и тип покупки).</param>
/// <param name="Currency">ISO 4217 код валюты, либо <c>"XTR"</c> для оплаты Telegram Stars (T-8.2/T-8.3) — в этом случае <see cref="ProviderToken"/> не нужен.</param>
/// <param name="ProviderToken">Токен платёжного провайдера (конфиг <c>Telegram:PaymentProviderToken</c>) — пустая строка при оплате в Stars.</param>
/// <param name="Prices">Разбивка суммы на позиции — Telegram Bot API требует хотя бы одну; итоговая сумма — их сумма.</param>
public sealed record TelegramInvoice(
    string Title,
    string Description,
    string Payload,
    string Currency,
    string ProviderToken,
    IReadOnlyList<TelegramLabeledPrice> Prices);

/// <param name="Amount">Сумма в минимальных единицах валюты (копейки/центы), либо количество Stars при <c>Currency == "XTR"</c>.</param>
public sealed record TelegramLabeledPrice(string Label, int Amount);

/// <param name="PhotoUrls">Прямые HTTPS-ссылки на файлы (хост <c>api.telegram.org</c>) — действительны ограниченное время, скачивать нужно сразу, не сохранять как постоянный URL.</param>
public sealed record TelegramUserProfilePhotos(int TotalCount, IReadOnlyList<Uri> PhotoUrls);
