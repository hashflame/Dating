using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.App.Telegram;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blizka.IntegrationTests.Auth;

/// <summary>
/// Проверяет <see cref="TelegramAuthMiddleware"/> через реальный HTTP на минимальном тестовом хосте
/// (по аналогии с <c>BlizkaExceptionHandlerTests</c>), чтобы не зависеть от полной сборки Blizka.Host
/// (БД, CORS), которую потребовал бы <c>WebApplicationFactory&lt;Program&gt;</c>.
/// </summary>
public sealed class TelegramAuthMiddlewareTests : IAsyncLifetime
{
    private const string BotToken = "42:TEST-BOT-TOKEN";

    private IHost _host = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Telegram:BotToken"] = BotToken,
                    });
                });
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                });
                webBuilder.Configure(app =>
                {
                    app.UseMiddleware<TelegramAuthMiddleware>();

                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapPost("/api/auth/telegram", (HttpContext context) =>
                        {
                            var data = context.Items[TelegramAuthMiddleware.ItemsKey] as TelegramInitData;
                            return Results.Ok(new { telegramId = data?.TelegramId });
                        });
                        endpoints.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
                    });
                });
            })
            .StartAsync();

        _client = _host.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }

    [Fact(DisplayName = "КОГДА initData подписана корректно ТОГДА запрос доходит до эндпоинта с распарсенным payload")]
    public async Task Valid_initData_reaches_the_endpoint_with_parsed_payload()
    {
        var initData = BuildSignedInitData(BotToken, DateTimeOffset.UtcNow, telegramId: 777, firstName: "Ann");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/telegram");
        request.Headers.Add(TelegramAuthMiddleware.HeaderName, initData);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElementResult>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.Equal(777, body!.TelegramId);
    }

    [Fact(DisplayName = "КОГДА заголовок initData отсутствует ТОГДА запрос отклоняется с 401")]
    public async Task Missing_header_is_rejected_with_401()
    {
        var response = await _client.PostAsync("/api/auth/telegram", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("TELEGRAM_INIT_DATA_INVALID", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА initData подделана ТОГДА запрос отклоняется с 401")]
    public async Task Tampered_initData_is_rejected_with_401()
    {
        var initData = BuildSignedInitData(BotToken, DateTimeOffset.UtcNow, telegramId: 777, firstName: "Ann");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/telegram");
        request.Headers.Add(TelegramAuthMiddleware.HeaderName, initData.Replace("Ann", "Eve"));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА маршрут не относится к auth ТОГДА запрос проходит без заголовка initData")]
    public async Task Other_routes_pass_through_without_requiring_the_header()
    {
        var response = await _client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string BuildSignedInitData(string botToken, DateTimeOffset authDate, long telegramId, string firstName)
    {
        var userJson = $$"""{"id":{{telegramId}},"first_name":"{{firstName}}"}""";

        var fields = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_date"] = authDate.ToUnixTimeSeconds().ToString(),
            ["user"] = userJson,
        };

        var dataCheckString = string.Join('\n', fields.Select(f => $"{f.Key}={f.Value}"));

        var secretKey = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));
        var hash = Convert.ToHexStringLower(HMACSHA256.HashData(secretKey, Encoding.UTF8.GetBytes(dataCheckString)));

        var queryFields = fields.ToDictionary(f => f.Key, f => f.Value);
        queryFields["hash"] = hash;

        return string.Join('&', queryFields.Select(f => $"{f.Key}={Uri.EscapeDataString(f.Value)}"));
    }

    private sealed record JsonElementResult(long? TelegramId);
}
