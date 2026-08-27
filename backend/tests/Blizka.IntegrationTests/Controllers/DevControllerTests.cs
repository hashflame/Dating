using System.Net;
using System.Net.Http.Json;
using Blizka.Api;
using Blizka.Api.Auth;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.App;
using Blizka.App.Domain.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blizka.IntegrationTests.Controllers;

/// <summary>
/// Проверяет <see cref="Blizka.Api.Controllers.DevController"/> через реальный HTTP-конвейер, но с фейковым
/// <see cref="IDemoSeedService"/> вместо <c>Blizka.Data.DevSeed.DemoSeedService</c>/Postgres — по тому же
/// минимальному тестовому хосту, что и <see cref="UsersControllerTests"/>.
/// </summary>
public sealed class DevControllerTests : IAsyncLifetime
{
    private const string ConfiguredSecret = "test-only-dev-login-secret";

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeDemoSeedService _demoSeedService = null!;

    public async Task InitializeAsync()
    {
        _demoSeedService = new FakeDemoSeedService();

        _host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = "test-only-secret-not-used-outside-this-test-host",
                        ["DevLogin:Secret"] = ConfiguredSecret,
                    });
                });
                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddApiLayer(context.Configuration);
                    services.AddAppLayer(context.Configuration);
                    services.AddSingleton<IDemoSeedService>(_demoSeedService);
                    services.AddExceptionHandler<BlizkaExceptionHandler>();
                    services.AddProblemDetails();
                });
                webBuilder.Configure(app =>
                {
                    app.UseExceptionHandler();
                    app.UseRouting();
                    app.UseAuthentication();
                    app.UseAuthorization();
                    app.UseEndpoints(endpoints => endpoints.MapControllers());
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

    [Fact(DisplayName = "КОГДА секрет верный ТОГДА reseed-demo-data пересоздаёт демо-пользователей и возвращает их список")]
    public async Task Reseed_with_the_correct_secret_returns_200_with_demo_users()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/dev/reseed-demo-data");
        request.Headers.Add(TelegramAuthMiddleware.DevLoginSecretHeaderName, ConfiguredSecret);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, _demoSeedService.ReseedCallCount);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ReseedResponseBody>>();
        Assert.Single(body!.Data.Users);
        Assert.Equal(990_000_000_001, body.Data.Users[0].TelegramId);
    }

    [Fact(DisplayName = "КОГДА секрет отсутствует ТОГДА reseed-demo-data отклоняется с 401 и сервис не вызывается")]
    public async Task Reseed_without_a_secret_header_returns_401_and_does_not_call_the_service()
    {
        var response = await _client.PostAsync("/api/dev/reseed-demo-data", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, _demoSeedService.ReseedCallCount);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("DEV_ACCESS_DENIED", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА секрет неверный ТОГДА reseed-demo-data отклоняется с 401 и сервис не вызывается")]
    public async Task Reseed_with_the_wrong_secret_returns_401_and_does_not_call_the_service()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/dev/reseed-demo-data");
        request.Headers.Add(TelegramAuthMiddleware.DevLoginSecretHeaderName, "wrong-secret");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, _demoSeedService.ReseedCallCount);
    }

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА reset-my-state отклоняется с 401, а не 500 (баг из e2e-прогона: классовый AllowAnonymous перебивал [Authorize] метода)")]
    public async Task ResetMyState_without_a_token_returns_401_not_500()
    {
        var response = await _client.PostAsync("/api/dev/reset-my-state", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА передан мусорный токен ТОГДА reset-my-state отклоняется с 401, а не 500")]
    public async Task ResetMyState_with_a_garbage_token_returns_401_not_500()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/dev/reset-my-state");
        request.Headers.Add("Authorization", "Bearer garbage.not.a.jwt");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record ReseedResponseBody(IReadOnlyList<ReseedUserBody> Users);

    private sealed record ReseedUserBody(long TelegramId, string Username, string Name, string? MainPhotoUrl);

    private sealed class FakeDemoSeedService : IDemoSeedService
    {
        public int ReseedCallCount { get; private set; }

        public Task<IReadOnlyList<DemoSeedResultUser>> ReseedAsync(CancellationToken cancellationToken)
        {
            ReseedCallCount++;
            IReadOnlyList<DemoSeedResultUser> result = [new DemoSeedResultUser(990_000_000_001, "demo_user_1", "Алина Демо", "https://example.invalid/photo.jpg")];
            return Task.FromResult(result);
        }
    }
}
