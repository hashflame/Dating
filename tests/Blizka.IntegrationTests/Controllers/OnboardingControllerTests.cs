using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Onboarding;
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
/// Проверяет <see cref="Blizka.Api.Controllers.OnboardingController"/> через реальный HTTP-конвейер —
/// JWT bearer, [Authorize], MediatR, FluentValidation — но с фейковыми репозиториями вместо
/// Blizka.Data/Postgres (по тому же принципу минимального тестового хоста, что и
/// TelegramAuthMiddlewareTests/BlizkaExceptionHandlerTests). Это первый [Authorize]-контроллер
/// в проекте, поэтому важно проверить именно проводку через реальный JWT bearer handler
/// (а не только <c>ClaimsPrincipalExtensions.GetUserId()</c> юнит-тестом).
/// </summary>
public sealed class OnboardingControllerTests : IAsyncLifetime
{
    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeOnboardingDraftRepository _draftRepository = null!;

    public async Task InitializeAsync()
    {
        _draftRepository = new FakeOnboardingDraftRepository();

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
                    services.AddSingleton<IOnboardingDraftRepository>(_draftRepository);
                    services.AddSingleton<ICityRepository>(new FakeCityRepository());
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

    private string IssueToken(Guid userId)
    {
        var jwtTokenService = _host.Services.GetRequiredService<IJwtTokenService>();
        var user = new User { Id = userId, TelegramId = 1, Locale = "ru", Status = UserStatus.New };
        return jwtTokenService.IssueToken(user).Token;
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
    }
}
