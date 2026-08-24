using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blizka.Api;
using Blizka.Api.Common;
using Blizka.Api.ErrorHandling;
using Blizka.Api.Matches;
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

/// <summary>Проверяет <see cref="Blizka.Api.Controllers.MatchesController"/> (T-7.1) по тому же минимальному тестовому хосту, что и <see cref="LikesControllerTests"/>.</summary>
public sealed class MatchesControllerTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions ResponseJsonOptions = CreateResponseJsonOptions();

    private IHost _host = null!;
    private HttpClient _client = null!;
    private FakeMatchRepository _matchRepository = null!;

    public async Task InitializeAsync()
    {
        _matchRepository = new FakeMatchRepository();

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
                    });
                });
                webBuilder.ConfigureServices((context, services) =>
                {
                    services.AddApiLayer(context.Configuration);
                    services.AddAppLayer(context.Configuration);
                    services.AddSingleton<IMatchRepository>(_matchRepository);
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

    [Fact(DisplayName = "КОГДА запрос без токена ТОГДА GET /api/matches отклоняется с 401")]
    public async Task GetMatches_without_token_returns_401()
    {
        var response = await _client.GetAsync("/api/matches");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА есть мэтчи во всех трёх секциях ТОГДА GET /api/matches возвращает new/waitingForMessage/archived с бейджами")]
    public async Task GetMatches_returns_all_three_sections()
    {
        var currentUserId = Guid.NewGuid();
        var newPartner = CreateUser("Anna");
        var waitingPartner = CreateUser("Vera");
        var archivedPartner = CreateUser("Nika");

        var newMatch = CreateMatch(currentUserId, newPartner, matchedAt: DateTimeOffset.UtcNow);
        var waitingMatch = CreateMatch(currentUserId, waitingPartner, matchedAt: DateTimeOffset.UtcNow.AddDays(-3));
        waitingMatch.ContactUnlockedAt = DateTimeOffset.UtcNow.AddDays(-2);
        var archivedMatch = CreateMatch(currentUserId, archivedPartner, matchedAt: DateTimeOffset.UtcNow.AddDays(-10));
        archivedMatch.Status = MatchStatus.Archived;

        _matchRepository.New = [newMatch];
        _matchRepository.WaitingForMessage = [waitingMatch];
        _matchRepository.Archived = [archivedMatch];

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/matches");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MatchesResponse>>(ResponseJsonOptions);

        var newItem = Assert.Single(body!.Data.New);
        Assert.Equal("Anna", newItem.User.Name);
        Assert.Equal(1, newItem.ContactCost);
        Assert.False(newItem.WritesFirst);

        var waitingItem = Assert.Single(body.Data.WaitingForMessage);
        Assert.Equal("Vera", waitingItem.User.Name);
        Assert.Equal("contact_opened", waitingItem.Badge);

        var archivedItem = Assert.Single(body.Data.Archived);
        Assert.Equal("Nika", archivedItem.User.Name);
        Assert.Equal("no_activity_7_days", archivedItem.Reason);
    }

    private static User CreateUser(string name) => new()
    {
        Id = Guid.NewGuid(),
        TelegramId = Random.Shared.NextInt64(),
        Status = UserStatus.Active,
        Name = name,
        BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
        Gender = Gender.Female,
        Locale = "ru",
        CreatedAt = DateTimeOffset.UtcNow,
        UpdatedAt = DateTimeOffset.UtcNow,
    };

    private static Match CreateMatch(Guid currentUserId, User other, DateTimeOffset matchedAt)
    {
        var currentUser = new User { Id = currentUserId, Name = "Me", BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)), Gender = Gender.Male, Locale = "ru" };
        var (user1, user2) = currentUserId.CompareTo(other.Id) < 0 ? (currentUser, other) : (other, currentUser);

        return new Match
        {
            Id = Guid.NewGuid(),
            User1Id = user1.Id,
            User1 = user1,
            User2Id = user2.Id,
            User2 = user2,
            Status = MatchStatus.Active,
            MatchedAt = matchedAt,
        };
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

    private sealed class FakeMatchRepository : IMatchRepository
    {
        public IReadOnlyList<Match> New { get; set; } = [];

        public IReadOnlyList<Match> WaitingForMessage { get; set; } = [];

        public IReadOnlyList<Match> Archived { get; set; } = [];

        public Task AddAsync(Match match, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списка мэтчей.");

        public Task<Match?> GetByUsersAsync(Guid userId1, Guid userId2, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах списка мэтчей.");

        public void Remove(Match match) =>
            throw new NotSupportedException("Не используется в тестах списка мэтчей.");

        public Task<IReadOnlyList<Match>> GetNewAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(New);

        public Task<IReadOnlyList<Match>> GetWaitingForMessageAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(WaitingForMessage);

        public Task<IReadOnlyList<Match>> GetArchivedAsync(Guid userId, CancellationToken cancellationToken) =>
            Task.FromResult(Archived);
    }
}
