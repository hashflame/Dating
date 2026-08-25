using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.Consent;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Users;
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
    private FakeUserRepository _userRepository = null!;
    private FakeSparkTransactionRepository _sparkTransactionRepository = null!;

    public async Task InitializeAsync()
    {
        _consentRepository = new FakeUserConsentRepository();
        _userRepository = new FakeUserRepository();
        _sparkTransactionRepository = new FakeSparkTransactionRepository();

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
                    services.AddSingleton<IUserConsentRepository>(_consentRepository);
                    services.AddSingleton<IUserRepository>(_userRepository);
                    services.AddSingleton<IUserDatePreferenceRepository>(new FakeUserDatePreferenceRepository());
                    services.AddSingleton<ISparkTransactionRepository>(_sparkTransactionRepository);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА GET /api/users/me отклоняется с 401")]
    public async Task GetMe_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА пользователь аутентифицирован ТОГДА GET /api/users/me возвращает id, telegramId, имя, баланс зорок, статус и completeness")]
    public async Task GetMe_with_valid_token_returns_the_users_profile()
    {
        var userId = Guid.NewGuid();
        const long telegramId = 555;
        var token = IssueToken(userId, telegramId);
        var user = Assert.Single(_userRepository.Users, u => u.Id == userId);
        user.Name = "Ann";
        user.SparksBalance = 42;
        user.Status = UserStatus.Active;
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserMeResponse>>(ResponseJsonOptions);
        Assert.Equal(userId, body!.Data.Id);
        Assert.Equal(telegramId, body.Data.TelegramId);
        Assert.Equal("Ann", body.Data.Name);
        Assert.Equal(42, body.Data.SparksBalance);
        Assert.Equal(UserStatus.Active, body.Data.Status);
        Assert.Equal(35, body.Data.ProfileCompleteness);
    }

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА PATCH /api/users/me/profile отклоняется с 401")]
    public async Task PatchProfile_without_token_returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/users/me/profile")
        {
            Content = JsonContent.Create(new { name = "Ann" }),
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА переданы валидные поля ТОГДА PATCH /api/users/me/profile обновляет профиль и возвращает его")]
    public async Task PatchProfile_with_valid_body_updates_the_profile()
    {
        var userId = Guid.NewGuid();
        var token = IssueToken(userId, 555);
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/users/me/profile")
        {
            Content = JsonContent.Create(new { name = "Bob", bio = "Hi", height = 180, datingGoal = "casual" }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PatchUserProfileResponse>>(ResponseJsonOptions);
        Assert.Equal("Bob", body!.Data.Profile.Name);
        Assert.Equal("Hi", body.Data.Profile.Bio);
        Assert.Equal(180, body.Data.Profile.Height);
        var user = Assert.Single(_userRepository.Users, u => u.Id == userId);
        Assert.Equal("Bob", user.Name);
    }

    [Fact(DisplayName = "КОГДА имя длиннее 30 символов ТОГДА PATCH /api/users/me/profile отвечает 400 VALIDATION_ERROR")]
    public async Task PatchProfile_with_a_too_long_name_returns_400_validation_error()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/users/me/profile")
        {
            Content = JsonContent.Create(new { name = new string('a', 31) }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid(), 1));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА GET /api/users/me/preview отклоняется с 401")]
    public async Task GetProfilePreview_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/users/me/preview");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА пользователь аутентифицирован ТОГДА GET /api/users/me/preview возвращает карточку профиля")]
    public async Task GetProfilePreview_with_valid_token_returns_the_preview_card()
    {
        var userId = Guid.NewGuid();
        var token = IssueToken(userId, 555);
        var user = Assert.Single(_userRepository.Users, u => u.Id == userId);
        user.Name = "Ann";
        user.BirthDate = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date.AddYears(-25));
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me/preview");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ProfilePreviewResponse>>(ResponseJsonOptions);
        Assert.Equal(userId, body!.Data.UserId);
        Assert.Equal("Ann", body.Data.Name);
        Assert.Equal(25, body.Data.Age);
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
            Content = JsonContent.Create(new { type = "termsAndPrivacyPolicy", version = "1.0", ageConfirmed = true }),
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
            Content = JsonContent.Create(new { type = "termsAndPrivacyPolicy", version = "", ageConfirmed = true }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid(), 1));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА ageConfirmed не передан для termsAndPrivacyPolicy ТОГДА ответ 400 VALIDATION_ERROR (spec 002, B4)")]
    public async Task RecordConsent_without_age_confirmation_returns_400_validation_error()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/consent")
        {
            Content = JsonContent.Create(new { type = "termsAndPrivacyPolicy", version = "1.0", ageConfirmed = false }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid(), 1));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
        Assert.Empty(_consentRepository.Consents);
    }

    [Fact(DisplayName = "КОГДА версия документа длиннее 32 символов (лимит колонки UserConsent.Version) ТОГДА ответ 400 VALIDATION_ERROR, а не 500")]
    public async Task RecordConsent_with_version_longer_than_the_column_limit_returns_400_validation_error()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/consent")
        {
            Content = JsonContent.Create(new { type = "termsAndPrivacyPolicy", version = new string('1', 33), ageConfirmed = true }),
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА статус согласий отклоняется с 401")]
    public async Task GetConsentStatus_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/users/me/consent");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА согласий ещё не было ТОГДА GET возвращает Given=false, а не 404")]
    public async Task GetConsentStatus_returns_given_false_when_no_consent_recorded()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/users/me/consent");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid(), 1));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserConsentStatusResponse[]>>(ResponseJsonOptions);
        var status = Assert.Single(body!.Data);
        Assert.Equal(ConsentType.TermsAndPrivacyPolicy, status.Type);
        Assert.False(status.Given);
    }

    [Fact(DisplayName = "КОГДА согласие уже зафиксировано ТОГДА GET возвращает Given=true с версией и временем")]
    public async Task GetConsentStatus_returns_given_true_after_recording_consent()
    {
        var userId = Guid.NewGuid();
        var postRequest = new HttpRequestMessage(HttpMethod.Post, "/api/users/me/consent")
        {
            Content = JsonContent.Create(new { type = "termsAndPrivacyPolicy", version = "1.0", ageConfirmed = true }),
        };
        postRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(userId, 1));
        await _client.SendAsync(postRequest);

        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users/me/consent");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(userId, 1));
        var response = await _client.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UserConsentStatusResponse[]>>(ResponseJsonOptions);
        var status = Assert.Single(body!.Data);
        Assert.True(status.Given);
        Assert.Equal("1.0", status.Version);
    }

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА удаление аккаунта отклоняется с 401")]
    public async Task DeleteAccount_without_token_returns_401()
    {
        var response = await _client.DeleteAsync("/api/users/me/account");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА пользователь аутентифицирован ТОГДА DELETE /api/users/me/account помечает аккаунт удалённым и возвращает 204")]
    public async Task DeleteAccount_with_valid_token_marks_the_account_deleted()
    {
        var userId = Guid.NewGuid();
        var token = IssueToken(userId, 555);
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me/account");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var user = Assert.Single(_userRepository.Users, u => u.Id == userId);
        Assert.Equal(UserStatus.Deleted, user.Status);
        Assert.NotNull(user.DeletedAt);
    }

    [Fact(DisplayName = "КОГДА аккаунт уже удалён ТОГДА повторный DELETE тоже возвращает 204 (идемпотентность)")]
    public async Task DeleteAccount_called_twice_stays_idempotent()
    {
        var userId = Guid.NewGuid();
        var token = IssueToken(userId, 555);
        var firstRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me/account");
        firstRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await _client.SendAsync(firstRequest);

        var secondRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/users/me/account");
        secondRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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
        _userRepository.Users.RemoveAll(u => u.Id == userId);
        _userRepository.Users.Add(user);
        return jwtTokenService.IssueToken(user).Token;
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(u => u.TelegramId == telegramId));

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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

        public Task<List<UserConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Consents.Where(c => c.UserId == userId).ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUserDatePreferenceRepository : IUserDatePreferenceRepository
    {
        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(0);
    }

    private sealed class FakeSparkTransactionRepository : ISparkTransactionRepository
    {
        public List<SparkTransaction> Transactions { get; } = [];

        public Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<SparkTransaction>, int)>(([], 0));

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
