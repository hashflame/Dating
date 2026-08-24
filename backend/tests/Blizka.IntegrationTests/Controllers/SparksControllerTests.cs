using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Sparks;
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

/// <summary>Проверяет <see cref="Blizka.Api.Controllers.SparksController"/> (T-8.1) по тому же минимальному тестовому хосту, что и <see cref="MatchesControllerTests"/>.</summary>
public sealed class SparksControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = CreateResponseJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeUserRepository _userRepository = null!;
    private FakeSparkTransactionRepository _sparkTransactionRepository = null!;

    public async Task InitializeAsync()
    {
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
                    services.AddSingleton<IUserRepository>(_userRepository);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА GET /api/sparks/wallet отклоняется с 401")]
    public async Task GetWallet_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/sparks/wallet");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА пользователь авторизован ТОГДА GET /api/sparks/wallet возвращает баланс, историю и каталог начислений")]
    public async Task GetWallet_returns_balance_history_and_earn_options()
    {
        var user = CreateUser(sparksBalance: 42);
        _userRepository.Users.Add(user);
        _sparkTransactionRepository.Transactions.Add(new SparkTransaction
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Amount = 42,
            Type = SparkTransactionType.RegistrationBonus,
            BalanceAfter = 42,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sparks/wallet");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(user.Id));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<SparksWalletResponse>>(ResponseJsonOptions);

        Assert.Equal(42, body!.Data.Balance);
        Assert.Equal(1, body.Data.History.TotalCount);
        Assert.Equal(1, body.Data.History.Page);
        Assert.Equal(20, body.Data.History.PageSize);
        var item = Assert.Single(body.Data.History.Items);
        Assert.Equal(SparkTransactionType.RegistrationBonus, item.Type);
        Assert.Contains(body.Data.EarnOptions, o => o.Type == SparkTransactionType.RegistrationBonus && o.Amount == 50);
    }

    [Fact(DisplayName = "КОГДА передан pageSize вне диапазона 1-50 ТОГДА GET /api/sparks/wallet отвечает 400")]
    public async Task GetWallet_returns_400_for_an_out_of_range_page_size()
    {
        var user = CreateUser(sparksBalance: 0);
        _userRepository.Users.Add(user);

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sparks/wallet?pageSize=100");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(user.Id));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static User CreateUser(int sparksBalance) => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = "Me",
        BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
        Gender = Gender.Female,
        Locale = "ru",
        SparksBalance = sparksBalance,
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

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

    private sealed class FakeUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task<User?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах кошелька.");

        public Task<User?> GetByIdWithProfileDataAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах кошелька.");

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Users.SingleOrDefault(u => u.Id == id));

        public Task AddAsync(User user, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах кошелька.");

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

        public Task<(IReadOnlyList<SparkTransaction> Items, int TotalCount)> GetHistoryAsync(
            Guid userId, int page, int pageSize, CancellationToken cancellationToken)
        {
            var query = Transactions.Where(t => t.UserId == userId).OrderByDescending(t => t.CreatedAt).ToList();
            var items = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return Task.FromResult<(IReadOnlyList<SparkTransaction>, int)>((items, query.Count));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
