using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Referrals;
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

/// <summary>Проверяет <see cref="Blizka.Api.Controllers.ReferralsController"/> (T-20.1) по тому же минимальному тестовому хосту, что и <see cref="SparksControllerTests"/>.</summary>
public sealed class ReferralsControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = CreateResponseJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeReferralRepository _referralRepository = null!;

    public async Task InitializeAsync()
    {
        _referralRepository = new FakeReferralRepository();

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
                        ["Referral:BotUsername"] = "blizka_bot",
                        ["Sparks:SuperlikeCost"] = "5",
                        ["Sparks:LikesRevealCost"] = "10",
                        ["Sparks:ContactUnlockCost"] = "1",
                        ["Sparks:RegistrationBonusAmount"] = "50",
                        ["Sparks:ProfileCompletionThresholdBonusAmount"] = "2",
                        ["Sparks:VerificationBonusAmount"] = "3",
                        ["Sparks:ReferralBonusAmount"] = "2",
                        ["Sparks:IdeaSubmissionBonusAmount"] = "1",
                        ["Sparks:IdeaImplementedBonusAmount"] = "10",
                    });
                });
                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddApiLayer(context.Configuration);
                    services.AddAppLayer(context.Configuration);
                    services.AddSingleton<IReferralRepository>(_referralRepository);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА POST /api/referrals/invite отклоняется с 401")]
    public async Task Invite_without_token_returns_401()
    {
        var response = await _client.PostAsync("/api/referrals/invite", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА пользователь авторизован ТОГДА POST /api/referrals/invite возвращает deepLink на blizka_bot с декодируемым кодом")]
    public async Task Invite_returns_a_deep_link_with_a_decodable_code()
    {
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/referrals/invite");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(userId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ReferralInviteResponse>>(ResponseJsonOptions);

        Assert.Equal($"https://t.me/blizka_bot?start=ref_{body!.Data.Code}", body.Data.DeepLink);
        Assert.Contains(body.Data.DeepLink, body.Data.ShareText);
    }

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА GET /api/referrals/stats отклоняется с 401")]
    public async Task Stats_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/referrals/stats");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА у пользователя есть приглашённые ТОГДА GET /api/referrals/stats возвращает invited/registered/sparksEarned")]
    public async Task Stats_returns_invited_registered_and_sparks_earned()
    {
        var userId = Guid.NewGuid();
        _referralRepository.Counts[userId] = (5, 3);
        _referralRepository.SparksEarned[userId] = 6;
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/referrals/stats");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(userId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<ReferralStatsResponse>>(ResponseJsonOptions);

        Assert.Equal(5, body!.Data.Invited);
        Assert.Equal(3, body.Data.Registered);
        Assert.Equal(6, body.Data.SparksEarned);
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

    private sealed class FakeReferralRepository : IReferralRepository
    {
        public Dictionary<Guid, (int Invited, int Registered)> Counts { get; } = [];

        public Dictionary<Guid, int> SparksEarned { get; } = [];

        public Task<Referral?> GetByReferredUserIdAsync(Guid referredUserId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах контроллера рефералов.");

        public Task AddAsync(Referral referral, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах контроллера рефералов.");

        public Task<(int Invited, int Registered)> GetCountsAsync(Guid referrerUserId, CancellationToken cancellationToken) =>
            Task.FromResult(Counts.GetValueOrDefault(referrerUserId));

        public Task<int> GetTotalSparksEarnedAsync(Guid referrerUserId, CancellationToken cancellationToken) =>
            Task.FromResult(SparksEarned.GetValueOrDefault(referrerUserId));
    }
}
