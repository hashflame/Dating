using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.Consent;
using Blizka.Api.ErrorHandling;
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

/// <summary>
/// Проверяет <see cref="Blizka.Api.Controllers.UsersController"/> через реальный HTTP-конвейер — JWT bearer,
/// [Authorize], MediatR, FluentValidation — но с фейковым репозиторием вместо Blizka.Data/Postgres, по тому
/// же минимальному тестовому хосту, что и <see cref="OnboardingControllerTests"/>.
/// </summary>
public sealed class UsersControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = CreateResponseJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeUserConsentRepository _consentRepository = null!;

    public async Task InitializeAsync()
    {
        _consentRepository = new FakeUserConsentRepository();

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
                    services.AddAppLayer();
                    services.AddSingleton<IUserConsentRepository>(_consentRepository);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА фиксация согласия отклоняется с 401")]
    public async Task RecordConsent_without_token_returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/consent")
        {
            Content = JsonContent.Create(new { type = "termsAndPrivacyPolicy", version = "1.0" }),
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА тело запроса валидно ТОГДА согласие сохраняется под userId и telegramId из JWT-claim'ов")]
    public async Task RecordConsent_with_valid_body_saves_consent_under_the_token_user()
    {
        var userId = Guid.NewGuid();
        const long telegramId = 987654321;
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/consent")
        {
            Content = JsonContent.Create(new { type = "termsAndPrivacyPolicy", version = "1.0" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(userId, telegramId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserConsentResponse>>(ResponseJsonOptions);
        Assert.Equal(ConsentType.TermsAndPrivacyPolicy, body!.Data.Type);
        Assert.Equal("1.0", body.Data.Version);
        var stored = Assert.Single(_consentRepository.Consents);
        Assert.Equal(userId, stored.UserId);
        Assert.Equal(telegramId, stored.TelegramId);
    }

    [Fact(DisplayName = "КОГДА версия документа не указана ТОГДА ответ 400 VALIDATION_ERROR")]
    public async Task RecordConsent_with_empty_version_returns_400_validation_error()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/consent")
        {
            Content = JsonContent.Create(new { type = "termsAndPrivacyPolicy", version = "" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid(), 1));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА версия документа длиннее 32 символов (лимит колонки UserConsent.Version) ТОГДА ответ 400 VALIDATION_ERROR, а не 500")]
    public async Task RecordConsent_with_version_longer_than_the_column_limit_returns_400_validation_error()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/consent")
        {
            Content = JsonContent.Create(new { type = "termsAndPrivacyPolicy", version = new string('1', 33) }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid(), 1));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА тип согласия не распознан ТОГДА ответ 400 VALIDATION_ERROR в едином формате ApiErrorResponse, а не ValidationProblemDetails по умолчанию")]
    public async Task RecordConsent_with_unrecognized_type_returns_400_in_the_api_error_response_shape()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/consent")
        {
            Content = JsonContent.Create(new { type = "bogusType", version = "1.0" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid(), 1));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
    }

    private static JsonSerializerOptions CreateResponseJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private string IssueToken(Guid userId, long telegramId)
    {
        var jwtTokenService = _host.Services.GetRequiredService<IJwtTokenService>();
        var user = new User { Id = userId, TelegramId = telegramId, Locale = "ru", Status = UserStatus.New };
        return jwtTokenService.IssueToken(user).Token;
    }

    private sealed class FakeUserConsentRepository : IUserConsentRepository
    {
        public List<UserConsent> Consents { get; } = [];

        public Task AddAsync(UserConsent consent, CancellationToken cancellationToken)
        {
            Consents.Add(consent);
            return Task.CompletedTask;
        }

        public Task<bool> HasConsentAsync(Guid userId, ConsentType type, CancellationToken cancellationToken) =>
            Task.FromResult(Consents.Any(c => c.UserId == userId && c.Type == type));

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
