using System.Net;
using System.Net.Http.Json;
using System.Threading.RateLimiting;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;

namespace Blizka.Data.Telegram;

/// <summary>
/// Реализация <see cref="ITelegramBotService"/> поверх HTTP Bot API (T-10.1). <paramref name="httpClient"/>
/// уже сконфигурирован в <c>DataServiceCollectionExtensions</c> с базовым адресом
/// <c>https://api.telegram.org/bot{BotToken}/</c> — методы этого класса вызывают его относительными путями.
/// <paramref name="rateLimiter"/> — общий на все вызовы этого сервиса лимитер под ключом <c>"telegram"</c>
/// (30 запросов/сек — лимит Telegram на отправку сообщений ботом, см. регистрацию в
/// <c>DataServiceCollectionExtensions</c>); не общий с лимитером Nominatim (<see cref="Geo.NominatimGeocoder"/>),
/// поэтому оба зарегистрированы как keyed-сервисы вместо одного <c>RateLimiter</c> на весь контейнер.
/// </summary>
public sealed class TelegramBotService(
    HttpClient httpClient,
    [FromKeyedServices("telegram")] RateLimiter rateLimiter,
    IOptions<TelegramOptions> options,
    ILogger<TelegramBotService> logger) : ITelegramBotService
{
    private static readonly ResiliencePipeline<HttpResponseMessage> RetryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
        .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
        {
            // 429 — именно тот транзиентный случай, ради которого вообще нужна retry-политика здесь: клиентский
            // лимитер (30/сек, см. DataServiceCollectionExtensions) снижает частоту 429, но не исключает их
            // полностью (всплеск, несколько инстансов бэкенда) — Telegram в этом случае отвечает реальным HTTP
            // 429, а не 200 с ok:false. 5xx — сбой на стороне Telegram, тоже стоит повторить.
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                .Handle<HttpRequestException>()
                .Handle<TaskCanceledException>()
                .HandleResult(r => r.StatusCode == HttpStatusCode.TooManyRequests || (int)r.StatusCode >= 500),
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromMilliseconds(300),
            OnRetry = args =>
            {
                // Ответ, забракованный ShouldHandle, дальше не используется — без явного Dispose он бы утёк:
                // HttpClient не знает, что мы вызовем callback повторно.
                args.Outcome.Result?.Dispose();
                return default;
            },
        })
        .Build();

    public async Task SendMessageAsync(long telegramId, string text, TelegramParseMode parseMode, CancellationToken cancellationToken)
    {
        var payload = new SendMessagePayload(telegramId, text, ToApiParseMode(parseMode));
        await CallAsync<SendMessagePayload, object>("sendMessage", payload, cancellationToken);
    }

    public async Task<string> CreateInvoiceLinkAsync(TelegramInvoice invoice, CancellationToken cancellationToken)
    {
        // Пустой ProviderToken — нормальное явное значение при оплате Stars (Currency == "XTR"), но если
        // вызывающий код просто не указал токен для фиатной валюты, подставляем сконфигурированный
        // Telegram:PaymentProviderToken, а не отправляем в Bot API заведомо невалидный invoice.
        var providerToken = string.IsNullOrEmpty(invoice.ProviderToken) && invoice.Currency != "XTR"
            ? options.Value.PaymentProviderToken
            : invoice.ProviderToken;

        var payload = new CreateInvoiceLinkPayload(
            invoice.Title,
            invoice.Description,
            invoice.Payload,
            providerToken,
            invoice.Currency,
            [.. invoice.Prices.Select(p => new LabeledPricePayload(p.Label, p.Amount))]);

        var link = await CallAsync<CreateInvoiceLinkPayload, string>("createInvoiceLink", payload, cancellationToken);
        return link ?? throw new TelegramApiException("createInvoiceLink", null, "empty invoice link in response");
    }

    public async Task<TelegramUserProfilePhotos> GetUserProfilePhotosAsync(long telegramId, int limit, CancellationToken cancellationToken)
    {
        var photos = await CallAsync<object, UserProfilePhotosResult>(
            $"getUserProfilePhotos?user_id={telegramId}&limit={limit}", null, cancellationToken);
        if (photos is null || photos.Photos.Count == 0)
        {
            return new TelegramUserProfilePhotos(0, []);
        }

        var urls = new List<Uri>(photos.Photos.Count);
        foreach (var sizes in photos.Photos)
        {
            // Telegram отдаёт размеры одной фотографии по возрастанию — последний элемент самый крупный,
            // он и нужен для импорта аватара (T-3.1), а не превью.
            var largest = sizes[^1];
            var fileUrl = await ResolveFileUrlAsync(largest.FileId, cancellationToken);
            if (fileUrl is not null)
            {
                urls.Add(fileUrl);
            }
        }

        return new TelegramUserProfilePhotos(photos.TotalCount, urls);
    }

    private async Task<Uri?> ResolveFileUrlAsync(string fileId, CancellationToken cancellationToken)
    {
        var file = await CallAsync<object, FileResult>($"getFile?file_id={fileId}", null, cancellationToken);
        return file?.FilePath is { Length: > 0 } filePath
            ? new Uri($"https://api.telegram.org/file/bot{options.Value.BotToken}/{filePath}")
            : null;
    }

    private async Task<TResult?> CallAsync<TPayload, TResult>(string method, TPayload? payload, CancellationToken cancellationToken)
    {
        using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);
        if (!lease.IsAcquired)
        {
            // Лимит 30 запросов/сек уже выбран — вместо накопления неограниченной очереди уведомлений
            // (см. QueueLimit в регистрации лимитера) сразу сигнализируем вызывающему коду об отказе.
            throw new TelegramApiException(method, null, "rate limit queue is full");
        }

        var response = await RetryPipeline.ExecuteAsync(
            async ct => payload is null
                ? await httpClient.GetAsync(method, ct)
                : await httpClient.PostAsJsonAsync(method, payload, ct),
            cancellationToken);

        using (response)
        {
            var body = await response.Content.ReadFromJsonAsync<TelegramApiResponse<TResult>>(cancellationToken);
            if (body is null || !body.Ok)
            {
                logger.LogWarning(
                    "Telegram Bot API method {Method} failed: errorCode={ErrorCode}, description={Description}",
                    method, body?.ErrorCode, body?.Description);
                throw new TelegramApiException(method, body?.ErrorCode, body?.Description);
            }

            return body.Result;
        }
    }

    private static string? ToApiParseMode(TelegramParseMode parseMode) => parseMode switch
    {
        TelegramParseMode.MarkdownV2 => "MarkdownV2",
        TelegramParseMode.Html => "HTML",
        _ => null,
    };
}
