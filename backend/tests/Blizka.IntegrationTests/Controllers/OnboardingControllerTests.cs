using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Onboarding;
using Blizka.App;
using Blizka.App.Auth;
using Blizka.App.Domain.Entities;
using Blizka.App.Domain.Enums;
using Blizka.App.Domain.Repositories;
using NetTopologySuite.Geometries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Blizka.IntegrationTests.Controllers;

/// <summary>
/// Проверяет <see cref="Blizka.Api.Controllers.OnboardingController"/> через реальный HTTP-конвейер —
/// JWT bearer, [Authorize], MediatR, FluentValidation — но с фейковыми репозиториями вместо
/// Blizka.Data/Postgres (по тому же принципу минимального тестового хоста, что и
/// TelegramAuthMiddlewareTests/BlizkaExceptionHandlerTests). Это первый [Authorize]-контроллер
/// в проекте, поэтому важно проверить именно проводку через реальный JWT bearer handler
/// (а не только <c>ClaimsPrincipalExtensions.GetUserId()</c> юнит-тестом).
/// </summary>
public sealed class OnboardingControllerTests : IAsyncLifetime
{
    // OnboardingCompleteResponse.UserStatus (spec 002, B9) — сервер сериализует camelCase-строкой,
    // клиенту теста нужен тот же конвертер (см. CitiesControllerTests).
    private static readonly JsonSerializerOptions ResponseJsonOptions = CreateResponseJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeOnboardingDraftRepository _draftRepository = null!;
    private FakeUserRepository _userRepository = null!;
    private FakeUserConsentRepository _consentRepository = null!;
    private FakeSparkTransactionRepository _sparkTransactionRepository = null!;
    private FakeUserFilterRepository _filterRepository = null!;

    public async Task InitializeAsync()
    {
        _draftRepository = new FakeOnboardingDraftRepository();
        _userRepository = new FakeUserRepository();
        _consentRepository = new FakeUserConsentRepository();
        _sparkTransactionRepository = new FakeSparkTransactionRepository();
        _filterRepository = new FakeUserFilterRepository();

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
                    services.AddSingleton<IOnboardingDraftRepository>(_draftRepository);
                    services.AddSingleton<ICityRepository>(new FakeCityRepository());
                    services.AddSingleton<IUserRepository>(_userRepository);
                    services.AddSingleton<IUserConsentRepository>(_consentRepository);
                    services.AddSingleton<IUserDatePreferenceRepository>(new FakeUserDatePreferenceRepository());
                    services.AddSingleton<ISparkTransactionRepository>(_sparkTransactionRepository);
                    services.AddSingleton<IUserFilterRepository>(_filterRepository);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА GET черновика отклоняется с 401")]
    public async Task GetDraft_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/onboarding/draft");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА у пользователя из токена ещё нет черновика ТОГДА GET возвращает шаг 0")]
    public async Task GetDraft_with_valid_token_and_no_draft_returns_step_zero()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/onboarding/draft");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<OnboardingDraftResponse>>();
        Assert.Equal(0, body!.Data.Step);
    }

    [Fact(DisplayName = "КОГДА PATCH шага 1 валиден ТОГДА черновик сохраняется под userId из JWT-claim'а")]
    public async Task PatchDraft_with_valid_step1_data_saves_the_draft_under_the_token_user()
    {
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/onboarding/draft")
        {
            Content = JsonContent.Create(new { step = 1, data = new { name = "Ann", birthDate = "2000-01-01", gender = "female" } }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(userId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<OnboardingDraftResponse>>();
        Assert.Equal(1, body!.Data.Step);
        Assert.Equal("Ann", body.Data.Data.GetProperty("name").GetString());
        var stored = Assert.Single(_draftRepository.Drafts);
        Assert.Equal(userId, stored.UserId);
    }

    [Fact(DisplayName = "КОГДА пользователь впервые вызывает PATCH черновика ТОГДА его статус переходит New -> Onboarding (spec 002, B8)")]
    public async Task PatchDraft_for_the_first_time_transitions_the_user_status_to_onboarding()
    {
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/onboarding/draft")
        {
            Content = JsonContent.Create(new { step = 1, data = new { name = "Ann", birthDate = "2000-01-01", gender = "female" } }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(userId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var storedUser = Assert.Single(_userRepository.Users, u => u.Id == userId);
        Assert.Equal(UserStatus.Onboarding, storedUser.Status);
    }

    [Fact(DisplayName = "КОГДА PATCH шага 1 не проходит валидацию ТОГДА ответ 400 VALIDATION_ERROR")]
    public async Task PatchDraft_with_underage_birthdate_returns_400_validation_error()
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/onboarding/draft")
        {
            Content = JsonContent.Create(new { step = 1, data = new { name = "Ann", birthDate = "2015-01-01", gender = "female" } }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(Guid.NewGuid()));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("VALIDATION_ERROR", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА завершение онбординга отклоняется с 401")]
    public async Task Complete_without_token_returns_401()
    {
        var response = await _client.PostAsync("/api/onboarding/complete", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА все условия выполнены ТОГДА завершение онбординга возвращает 200, начисленные зорки и completeness")]
    public async Task Complete_with_full_profile_and_consent_returns_200_and_awards_sparks()
    {
        var user = SeedUser(photoCount: 1);
        _draftRepository.Drafts.Add(FullDraft(user.Id));
        _consentRepository.Consents.Add(user.Id);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/onboarding/complete");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueTokenFor(user));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<OnboardingCompleteResponse>>(ResponseJsonOptions);
        Assert.Equal(50, body!.Data.SparksAwarded);
        Assert.Equal(35, body.Data.ProfileCompleteness);
        Assert.Equal(60, body.Data.NextReward!.Threshold);
        Assert.False(string.IsNullOrEmpty(body.Data.NextReward.Hint));
        Assert.Equal(UserStatus.Active, body.Data.UserStatus);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Single(_sparkTransactionRepository.Transactions);
    }

    [Fact(DisplayName = "КОГДА согласие не зафиксировано ТОГДА завершение онбординга отклоняется с 422 ONBOARDING_INCOMPLETE")]
    public async Task Complete_without_consent_returns_422()
    {
        var user = SeedUser(photoCount: 1);
        _draftRepository.Drafts.Add(FullDraft(user.Id));
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/onboarding/complete");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueTokenFor(user));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("ONBOARDING_INCOMPLETE", body!.Error.Code);
    }

    [Fact(DisplayName = "КОГДА онбординг уже завершён ТОГДА повторный вызов отклоняется с 409")]
    public async Task Complete_when_already_active_returns_409()
    {
        var user = SeedUser(photoCount: 1);
        user.Status = UserStatus.Active;
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/onboarding/complete");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueTokenFor(user));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.Equal("ONBOARDING_ALREADY_COMPLETED", body!.Error.Code);
    }

    private static OnboardingDraft FullDraft(Guid userId) => new()
    {
        UserId = userId,
        Step = 3,
        DataJson =
            """{"name":"Ann","birthDate":"2000-01-01","gender":"female","showGender":"male","ageRange":{"min":20,"max":35},"datingGoals":["casual"],"cityId":"11111111-1111-1111-1111-111111111111"}""",
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private User SeedUser(int photoCount)
    {
        // Onboarding — статус пользователя, уже сделавшего хотя бы один PATCH черновика (spec 002, B8);
        // именно в этом статусе POST /api/onboarding/complete и должен успешно срабатывать.
        var user = new User { Id = Guid.NewGuid(), TelegramId = 1, Locale = "ru", Status = UserStatus.Onboarding };
        for (var i = 0; i < photoCount; i++)
        {
            user.Photos.Add(new Photo
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Url = $"https://cdn.example.com/{i}.jpg",
                ThumbnailUrl = $"https://cdn.example.com/{i}-thumb.jpg",
                MediumUrl = $"https://cdn.example.com/{i}-medium.jpg",
                SortOrder = i,
                IsMain = i == 0,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        _userRepository.Users.Add(user);
        return user;
    }

    private string IssueToken(Guid userId)
    {
        var jwtTokenService = _host.Services.GetRequiredService<IJwtTokenService>();
        var user = new User { Id = userId, TelegramId = 1, Locale = "ru", Status = UserStatus.New };

        // PatchOnboardingDraftCommandHandler теперь читает пользователя, чтобы перевести New -> Onboarding
        // при первом PATCH (spec 002, B8) — без записи в репозитории это упало бы на "user not found".
        if (_userRepository.Users.All(u => u.Id != userId))
        {
            _userRepository.Users.Add(user);
        }

        return jwtTokenService.IssueToken(user).Token;
    }

    private string IssueTokenFor(User user)
    {
        var jwtTokenService = _host.Services.GetRequiredService<IJwtTokenService>();
        return jwtTokenService.IssueToken(user).Token;
    }

    private static JsonSerializerOptions CreateResponseJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private sealed class FakeOnboardingDraftRepository : IOnboardingDraftRepository
    {
        public List<OnboardingDraft> Drafts { get; } = [];

        public Task<OnboardingDraft?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Drafts.SingleOrDefault(d => d.UserId == userId));

        public Task AddAsync(OnboardingDraft draft, CancellationToken cancellationToken)
        {
            Drafts.Add(draft);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeCityRepository : ICityRepository
    {
        public Task<bool> ExistsAsync(Guid cityId, CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<City?> GetByIdAsync(Guid cityId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Получение города по id не используется в тестах онбординга.");

        public Task<IReadOnlyList<City>> SearchAsync(string query, CityLocale locale, int limit, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Поиск городов не используется в тестах онбординга.");

        public Task<City?> FindNearestAsync(Point location, double maxDistanceMeters, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Определение города по координатам не используется в тестах онбординга.");
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
        public HashSet<Guid> Consents { get; } = [];

        public Task AddAsync(UserConsent consent, CancellationToken cancellationToken)
        {
            Consents.Add(consent.UserId);
            return Task.CompletedTask;
        }

        public Task<bool> HasConsentAsync(Guid userId, ConsentType type, CancellationToken cancellationToken) =>
            Task.FromResult(Consents.Contains(userId));

        public Task<List<UserConsent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Список согласий не используется в тестах онбординга.");

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUserDatePreferenceRepository : IUserDatePreferenceRepository
    {
        public Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken) => Task.FromResult(0);
    }

    private sealed class FakeUserFilterRepository : IUserFilterRepository
    {
        public UserFilter? AddedFilter { get; private set; }

        public Task<UserFilter?> GetAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(AddedFilter?.UserId == userId ? AddedFilter : null);

        public Task AddAsync(UserFilter filter, CancellationToken cancellationToken)
        {
            AddedFilter = filter;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeSparkTransactionRepository : ISparkTransactionRepository
    {
        public List<SparkTransaction> Transactions { get; } = [];

        public Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
