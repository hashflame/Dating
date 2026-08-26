using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Privacy;
using Blizka.App;
using Blizka.App.Auth;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blizka.IntegrationTests.Controllers;

/// <summary>Проверяет <see cref="Blizka.Api.Controllers.PrivacySettingsController"/> (T-16.1) по тому же минимальному тестовому хосту, что и <see cref="UsersControllerTests"/>.</summary>
public sealed class PrivacySettingsControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = CreateResponseJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakePrivacySettingsRepository _repository = null!;

    public async Task InitializeAsync()
    {
        _repository = new FakePrivacySettingsRepository();

        _host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureAppConfiguration(config =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Jwt:Secret"] = "test-only-secret-not-used-outside-this-test-host",
                        ["Jwt:Issuer"] = "blizka-tests",
                        ["Jwt:Audience"] = "blizka-tests-clients",
                    });
                });
                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddApiLayer(context.Configuration);
                    services.AddAppLayer(context.Configuration);
                    services.AddSingleton<IPrivacySettingsRepository>(_repository);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА GET /api/privacy/settings отклоняется с 401")]
    public async Task GetSettings_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/privacy/settings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА у пользователя ещё нет строки в БД ТОГДА GET возвращает дефолты, а не 404")]
    public async Task GetSettings_returns_defaults_when_none_are_stored()
    {
        var token = IssueToken(Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/privacy/settings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PrivacySettingsResponse>>(ResponseJsonOptions);
        Assert.False(body!.Data.BlockIncomingMessages);
        Assert.True(body.Data.ShowLastActive);
    }

    [Fact(DisplayName = "КОГДА PATCH передаёт только одно поле ТОГДА остальные не меняются и сохраняются в репозитории")]
    public async Task PatchSettings_updates_only_the_provided_field()
    {
        var userId = Guid.NewGuid();
        var token = IssueToken(userId);
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/privacy/settings")
        {
            Content = JsonContent.Create(new PatchPrivacySettingsRequest(true, null, null, null, null)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PrivacySettingsResponse>>(ResponseJsonOptions);
        Assert.True(body!.Data.BlockIncomingMessages);
        Assert.False(body.Data.HideDistance);
        var stored = Assert.Single(_repository.ByUserId.Values, s => s.UserId == userId);
        Assert.True(stored.BlockIncomingMessages);
    }

    [Fact(DisplayName = "КОГДА PATCH включает invisibleMode без подписки ТОГДА возвращается 422")]
    public async Task PatchSettings_rejects_invisible_mode_without_a_subscription()
    {
        var token = IssueToken(Guid.NewGuid());
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/privacy/settings")
        {
            Content = JsonContent.Create(new PatchPrivacySettingsRequest(null, null, null, null, true)),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private static JsonSerializerOptions CreateResponseJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private string IssueToken(Guid userId)
    {
        var jwtTokenService = _host.Services.GetRequiredService<IJwtTokenService>();
        var user = new User { Id = userId, TelegramId = 1, Locale = "ru", Status = UserStatus.Active };
        return jwtTokenService.IssueToken(user).Token;
    }

    private sealed class FakePrivacySettingsRepository : IPrivacySettingsRepository
    {
        public Dictionary<Guid, PrivacySettings> ByUserId { get; } = [];

        public Task<PrivacySettings?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(ByUserId.GetValueOrDefault(userId));

        public Task<PrivacySettings?> GetByUserIdTrackedAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(ByUserId.GetValueOrDefault(userId));

        public Task<IReadOnlyDictionary<Guid, PrivacySettings>> GetByUserIdsAsync(
            IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, PrivacySettings>>(
                ByUserId.Where(kv => userIds.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));

        public Task AddAsync(PrivacySettings settings, CancellationToken cancellationToken)
        {
            ByUserId[settings.UserId] = settings;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
