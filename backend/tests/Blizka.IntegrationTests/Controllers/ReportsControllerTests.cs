using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Reports;
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

/// <summary>Проверяет <see cref="Blizka.Api.Controllers.ReportsController"/> (T-17.1) по тому же минимальному тестовому хосту, что и <see cref="UserBlocksControllerTests"/>.</summary>
public sealed class ReportsControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions RequestJsonOptions = CreateRequestJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeUserRepository _userRepository = null!;
    private FakeReportRepository _reportRepository = null!;
    private FakeUserBlockRepository _blockRepository = null!;

    public async Task InitializeAsync()
    {
        _userRepository = new FakeUserRepository();
        _reportRepository = new FakeReportRepository();
        _blockRepository = new FakeUserBlockRepository();

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
                    services.AddSingleton<IUserRepository>(_userRepository);
                    services.AddSingleton<IReportRepository>(_reportRepository);
                    services.AddSingleton<IUserBlockRepository>(_blockRepository);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА POST /api/users/{userId}/report отклоняется с 401")]
    public async Task Report_without_token_returns_401()
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/users/{Guid.NewGuid()}/report", new CreateReportRequest(ReportReason.Spam, null, false), RequestJsonOptions);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА цель жалобы существует и причина обычная ТОГДА возвращается 204, жалоба сохраняется, аккаунт не блокируется")]
    public async Task Report_with_valid_target_returns_204()
    {
        var reporterId = Guid.NewGuid();
        var token = IssueToken(reporterId);
        var target = new User { Id = Guid.NewGuid(), TelegramId = 2, Name = "Target", Status = UserStatus.Active };
        _userRepository.Users.Add(target);

        var response = await SendReport(token, target.Id, new CreateReportRequest(ReportReason.Spam, "спамит рекламой", false));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var report = Assert.Single(_reportRepository.Reports);
        Assert.Equal(reporterId, report.ReporterUserId);
        Assert.Equal(target.Id, report.ReportedUserId);
        Assert.Equal(ReportReason.Spam, report.Reason);
        Assert.Equal(ReportPriority.Normal, report.Priority);
        Assert.Equal(UserStatus.Active, target.Status);
    }

    [Fact(DisplayName = "КОГДА причина жалобы критичная (underage) ТОГДА аккаунт блокируется немедленно")]
    public async Task Report_with_critical_reason_bans_immediately()
    {
        var token = IssueToken(Guid.NewGuid());
        var target = new User { Id = Guid.NewGuid(), TelegramId = 2, Name = "Target", Status = UserStatus.Active };
        _userRepository.Users.Add(target);

        var response = await SendReport(token, target.Id, new CreateReportRequest(ReportReason.Underage, null, false));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(UserStatus.Banned, target.Status);
        Assert.NotNull(target.BanReason);
    }

    [Fact(DisplayName = "КОГДА blockUser=true ТОГДА одновременно с жалобой сохраняется блокировка")]
    public async Task Report_with_block_flag_also_blocks_the_target()
    {
        var reporterId = Guid.NewGuid();
        var token = IssueToken(reporterId);
        var target = new User { Id = Guid.NewGuid(), TelegramId = 2, Name = "Target", Status = UserStatus.Active };
        _userRepository.Users.Add(target);

        var response = await SendReport(token, target.Id, new CreateReportRequest(ReportReason.Insults, null, true));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(_blockRepository.Blocks, b => b.BlockerUserId == reporterId && b.BlockedUserId == target.Id);
    }

    [Fact(DisplayName = "КОГДА цель жалобы не найдена ТОГДА возвращается 404")]
    public async Task Report_with_missing_target_returns_404()
    {
        var token = IssueToken(Guid.NewGuid());

        var response = await SendReport(token, Guid.NewGuid(), new CreateReportRequest(ReportReason.Spam, null, false));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА пользователь жалуется на самого себя ТОГДА возвращается 400")]
    public async Task Report_self_returns_400()
    {
        var userId = Guid.NewGuid();
        var token = IssueToken(userId);

        var response = await SendReport(token, userId, new CreateReportRequest(ReportReason.Spam, null, false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendReport(string token, Guid userId, CreateReportRequest body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/users/{userId}/report")
        {
            Content = JsonContent.Create(body, options: RequestJsonOptions),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await _client.SendAsync(request);
    }

    private static JsonSerializerOptions CreateRequestJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private string IssueToken(Guid userId)
    {
        var jwtTokenService = _host.Services.GetRequiredService<IJwtTokenService>();
        var user = new User { Id = userId, TelegramId = 1, Locale = "ru", Status = UserStatus.Active };
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

    private sealed class FakeReportRepository : IReportRepository
    {
        public List<Report> Reports { get; } = [];

        public Task AddAsync(Report report, CancellationToken cancellationToken)
        {
            Reports.Add(report);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Guid>> GetUsersExceedingReportThresholdAsync(
            DateTimeOffset since, int thresholdCount, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>(
                Reports.Where(r => r.CreatedAt >= since && r.Status == ReportStatus.Pending)
                    .Select(r => new { r.ReportedUserId, r.ReporterUserId })
                    .Distinct()
                    .GroupBy(r => r.ReportedUserId)
                    .Where(g => g.Count() >= thresholdCount)
                    .Select(g => g.Key)
                    .ToList());

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUserBlockRepository : IUserBlockRepository
    {
        public List<UserBlock> Blocks { get; } = [];

        public Task<bool> ExistsAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken) =>
            Task.FromResult(Blocks.Any(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId));

        public Task<bool> ExistsEitherDirectionAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken) =>
            Task.FromResult(Blocks.Any(b =>
                (b.BlockerUserId == userId && b.BlockedUserId == otherUserId) ||
                (b.BlockerUserId == otherUserId && b.BlockedUserId == userId)));

        public Task<IReadOnlyList<UserBlock>> GetBlockedByUserAsync(Guid blockerUserId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<UserBlock>>(Blocks.Where(b => b.BlockerUserId == blockerUserId).ToList());

        public Task AddAsync(UserBlock block, CancellationToken cancellationToken)
        {
            Blocks.Add(block);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Guid blockerUserId, Guid blockedUserId, CancellationToken cancellationToken)
        {
            Blocks.RemoveAll(b => b.BlockerUserId == blockerUserId && b.BlockedUserId == blockedUserId);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
