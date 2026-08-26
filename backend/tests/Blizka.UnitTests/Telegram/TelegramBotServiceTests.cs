using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Blizka.App.Domain.Exceptions;
using Blizka.App.Domain.Services;
using Blizka.Data.Telegram;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Blizka.UnitTests.Telegram;

public sealed class TelegramBotServiceTests
{
    [Fact(DisplayName = "КОГДА SendMessageAsync получает ok:true ТОГДА запрос уходит на sendMessage с chat_id/text/parse_mode")]
    public async Task SendMessageAsync_posts_expected_payload_on_success()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{"ok":true,"result":{}}"""));
        var service = CreateService(handler);

        await service.SendMessageAsync(42, "hello", TelegramParseMode.Html, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("sendMessage", request.RequestUri!.ToString());
        var body = JsonDocument.Parse(request.Body!);
        Assert.Equal(42, body.RootElement.GetProperty("chat_id").GetInt64());
        Assert.Equal("hello", body.RootElement.GetProperty("text").GetString());
        Assert.Equal("HTML", body.RootElement.GetProperty("parse_mode").GetString());
    }

    [Fact(DisplayName = "КОГДА Telegram Bot API отвечает ok:false ТОГДА SendMessageAsync выбрасывает TelegramApiException с errorCode и description")]
    public async Task SendMessageAsync_throws_TelegramApiException_on_api_error()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse("""{"ok":false,"error_code":400,"description":"chat not found"}"""));
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<TelegramApiException>(
            () => service.SendMessageAsync(42, "hello", TelegramParseMode.None, CancellationToken.None));

        Assert.Equal("sendMessage", exception.Method);
        Assert.Equal(400, exception.TelegramErrorCode);
    }

    [Fact(DisplayName = "КОГДА первая попытка падает с сетевой ошибкой ТОГДА retry-политика повторяет запрос и возвращает успех")]
    public async Task SendMessageAsync_retries_transient_network_failure()
    {
        var attempt = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempt++;
            if (attempt == 1)
            {
                throw new HttpRequestException("connection reset");
            }

            return JsonResponse("""{"ok":true,"result":{}}""");
        });
        var service = CreateService(handler);

        await service.SendMessageAsync(42, "hello", TelegramParseMode.None, CancellationToken.None);

        Assert.Equal(2, attempt);
    }

    [Fact(DisplayName = "КОГДА CreateInvoiceLinkAsync получает ok:true ТОГДА возвращается ссылка из result")]
    public async Task CreateInvoiceLinkAsync_returns_link_from_result()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse("""{"ok":true,"result":"https://t.me/invoice/abc"}"""));
        var service = CreateService(handler);

        var invoice = new TelegramInvoice(
            "Зорки", "100 зорок", "payload-1", "XTR", string.Empty,
            [new TelegramLabeledPrice("100 зорок", 100)]);

        var link = await service.CreateInvoiceLinkAsync(invoice, CancellationToken.None);

        Assert.Equal("https://t.me/invoice/abc", link);
    }

    [Fact(DisplayName = "КОГДА CreateInvoiceLinkAsync получает пустой result ТОГДА выбрасывается TelegramApiException")]
    public async Task CreateInvoiceLinkAsync_throws_when_result_is_empty()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{"ok":true,"result":null}"""));
        var service = CreateService(handler);
        var invoice = new TelegramInvoice("t", "d", "p", "XTR", string.Empty, [new TelegramLabeledPrice("l", 1)]);

        await Assert.ThrowsAsync<TelegramApiException>(
            () => service.CreateInvoiceLinkAsync(invoice, CancellationToken.None));
    }

    [Fact(DisplayName = "КОГДА CreateInvoiceLinkAsync вызван для фиатной валюты без ProviderToken ТОГДА подставляется Telegram:PaymentProviderToken из конфига")]
    public async Task CreateInvoiceLinkAsync_falls_back_to_configured_provider_token_for_fiat_currency()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{"ok":true,"result":"https://t.me/invoice/abc"}"""));
        var service = CreateService(handler, paymentProviderToken: "configured-provider-token");
        var invoice = new TelegramInvoice("t", "d", "p", "BYN", string.Empty, [new TelegramLabeledPrice("l", 100)]);

        await service.CreateInvoiceLinkAsync(invoice, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        var body = JsonDocument.Parse(request.Body!);
        Assert.Equal("configured-provider-token", body.RootElement.GetProperty("provider_token").GetString());
    }

    [Fact(DisplayName = "КОГДА CreateInvoiceLinkAsync вызван для Stars (XTR) без ProviderToken ТОГДА provider_token остаётся пустым, а не подставляется из конфига")]
    public async Task CreateInvoiceLinkAsync_keeps_empty_provider_token_for_stars_currency()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{"ok":true,"result":"https://t.me/invoice/abc"}"""));
        var service = CreateService(handler, paymentProviderToken: "configured-provider-token");
        var invoice = new TelegramInvoice("t", "d", "p", "XTR", string.Empty, [new TelegramLabeledPrice("l", 100)]);

        await service.CreateInvoiceLinkAsync(invoice, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        var body = JsonDocument.Parse(request.Body!);
        Assert.Equal(string.Empty, body.RootElement.GetProperty("provider_token").GetString());
    }

    [Fact(DisplayName = "КОГДА у пользователя нет фото ТОГДА GetUserProfilePhotosAsync возвращает пустой список без обращения к getFile")]
    public async Task GetUserProfilePhotosAsync_returns_empty_when_no_photos()
    {
        var handler = new StubHttpMessageHandler(_ =>
            JsonResponse("""{"ok":true,"result":{"total_count":0,"photos":[]}}"""));
        var service = CreateService(handler);

        var result = await service.GetUserProfilePhotosAsync(42, 1, CancellationToken.None);

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.PhotoUrls);
        Assert.Single(handler.Requests);
    }

    [Fact(DisplayName = "КОГДА у пользователя есть фото ТОГДА GetUserProfilePhotosAsync берёт самый крупный размер и резолвит его в ссылку через getFile")]
    public async Task GetUserProfilePhotosAsync_resolves_largest_size_to_file_url()
    {
        var handler = new StubHttpMessageHandler(request =>
        {
            var uri = request.RequestUri!.ToString();
            if (uri.Contains("getUserProfilePhotos"))
            {
                return JsonResponse("""
                    {"ok":true,"result":{"total_count":1,"photos":[[
                        {"file_id":"small","width":100,"height":100},
                        {"file_id":"large","width":640,"height":640}
                    ]]}}
                    """);
            }

            Assert.Contains("file_id=large", uri);
            return JsonResponse("""{"ok":true,"result":{"file_path":"photos/file_1.jpg"}}""");
        });
        var service = CreateService(handler, botToken: "bot-token-1");

        var result = await service.GetUserProfilePhotosAsync(42, 1, CancellationToken.None);

        var url = Assert.Single(result.PhotoUrls);
        Assert.Equal("https://api.telegram.org/file/botbot-token-1/photos/file_1.jpg", url.ToString());
    }

    [Fact(DisplayName = "КОГДА Telegram отвечает 429 Too Many Requests ТОГДА retry-политика повторяет запрос и возвращает успех")]
    public async Task SendMessageAsync_retries_on_429_too_many_requests()
    {
        var attempt = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempt++;
            return attempt == 1
                ? new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                {
                    Content = new StringContent(
                        """{"ok":false,"error_code":429,"description":"Too Many Requests"}""", Encoding.UTF8, "application/json"),
                }
                : JsonResponse("""{"ok":true,"result":{}}""");
        });
        var service = CreateService(handler);

        await service.SendMessageAsync(42, "hello", TelegramParseMode.None, CancellationToken.None);

        Assert.Equal(2, attempt);
    }

    [Fact(DisplayName = "КОГДА Telegram отвечает 429 на всех попытках ТОГДА после исчерпания retry выбрасывается TelegramApiException с кодом 429")]
    public async Task SendMessageAsync_throws_after_exhausting_retries_on_persistent_429()
    {
        var attempt = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            attempt++;
            return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
            {
                Content = new StringContent(
                    """{"ok":false,"error_code":429,"description":"Too Many Requests"}""", Encoding.UTF8, "application/json"),
            };
        });
        var service = CreateService(handler);

        var exception = await Assert.ThrowsAsync<TelegramApiException>(
            () => service.SendMessageAsync(42, "hello", TelegramParseMode.None, CancellationToken.None));

        Assert.Equal(429, exception.TelegramErrorCode);
        Assert.Equal(4, attempt); // 1 исходная попытка + 3 повтора (MaxRetryAttempts)
    }

    [Fact(DisplayName = "КОГДА очередь rate-лимитера заполнена ТОГДА SendMessageAsync выбрасывает TelegramApiException без обращения к HTTP")]
    public async Task SendMessageAsync_throws_when_rate_limiter_queue_is_exhausted()
    {
        var handler = new StubHttpMessageHandler(_ => JsonResponse("""{"ok":true,"result":{}}"""));
        var exhaustedLimiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
        exhaustedLimiter.AttemptAcquire(1); // выбираем единственное разрешённое окно, второй запрос сразу получит отказ
        var service = CreateService(handler, rateLimiter: exhaustedLimiter);

        await Assert.ThrowsAsync<TelegramApiException>(
            () => service.SendMessageAsync(42, "hello", TelegramParseMode.None, CancellationToken.None));

        Assert.Empty(handler.Requests);
    }

    private static TelegramBotService CreateService(
        StubHttpMessageHandler handler,
        string botToken = "test-token",
        string paymentProviderToken = "",
        RateLimiter? rateLimiter = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri($"https://api.telegram.org/bot{botToken}/"),
        };
        var options = Options.Create(new TelegramOptions { BotToken = botToken, PaymentProviderToken = paymentProviderToken });
        rateLimiter ??= new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1000,
            Window = TimeSpan.FromSeconds(1),
        });

        return new TelegramBotService(httpClient, rateLimiter, options, NullLogger<TelegramBotService>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed record CapturedRequest(HttpMethod Method, Uri? RequestUri, string? Body);

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<CapturedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new CapturedRequest(request.Method, request.RequestUri, body));
            return respond(request);
        }
    }
}
