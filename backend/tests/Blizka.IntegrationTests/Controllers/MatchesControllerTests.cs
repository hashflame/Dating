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
                    services.AddSingleton<ISparkTransactionRepository>(new FakeSparkTransactionRepository());
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

    [Fact(DisplayName = "КОГДА мэтч не найден или чужой ТОГДА GET /api/matches/{matchId} отвечает 404")]
    public async Task GetMatchHub_returns_404_when_the_match_does_not_belong_to_the_requesting_user()
    {
        var currentUserId = Guid.NewGuid();
        _matchRepository.ById = CreateMatch(Guid.NewGuid(), CreateUser("Anna"), DateTimeOffset.UtcNow);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/matches/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА контакт мэтча ещё не открыт ТОГДА хаб отдаёт locked без telegramUsername")]
    public async Task GetMatchHub_returns_locked_status_without_telegram_username()
    {
        var currentUserId = Guid.NewGuid();
        var partner = CreateUser("Anna");
        partner.TelegramUsername = "anna_k";
        var match = CreateMatch(currentUserId, partner, matchedAt: DateTimeOffset.UtcNow);
        _matchRepository.ById = match;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/matches/{match.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MatchHubResponse>>(ResponseJsonOptions);

        Assert.Equal("Anna", body!.Data.User.Name);
        Assert.Equal("locked", body.Data.ContactStatus);
        Assert.Null(body.Data.User.TelegramUsername);
        Assert.False(body.Data.Features.QuestionOfDay.Available);
        Assert.False(body.Data.Features.Minigame.Available);
        Assert.False(body.Data.Features.DateIdea.Available);
        Assert.False(body.Data.Features.StaleConversation.Available);
    }

    [Fact(DisplayName = "КОГДА контакт мэтча открыт ТОГДА хаб отдаёт unlocked с telegramUsername")]
    public async Task GetMatchHub_returns_unlocked_status_with_telegram_username()
    {
        var currentUserId = Guid.NewGuid();
        var partner = CreateUser("Anna");
        partner.TelegramUsername = "anna_k";
        var match = CreateMatch(currentUserId, partner, matchedAt: DateTimeOffset.UtcNow);
        match.ContactUnlockedAt = DateTimeOffset.UtcNow;
        _matchRepository.ById = match;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/matches/{match.Id}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<MatchHubResponse>>(ResponseJsonOptions);

        Assert.Equal("unlocked", body!.Data.ContactStatus);
        Assert.Equal("anna_k", body.Data.User.TelegramUsername);
    }

    [Fact(DisplayName = "КОГДА мэтч не найден или чужой ТОГДА POST /unlock отвечает 404")]
    public async Task UnlockContact_returns_404_when_the_match_does_not_belong_to_the_requesting_user()
    {
        var currentUserId = Guid.NewGuid();
        _matchRepository.ById = CreateMatch(Guid.NewGuid(), CreateUser("Anna"), DateTimeOffset.UtcNow);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/matches/{Guid.NewGuid()}/unlock");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА баланса не хватает ТОГДА POST /unlock отвечает 402 и контакт остаётся закрыт")]
    public async Task UnlockContact_returns_402_when_the_balance_is_insufficient()
    {
        var currentUserId = Guid.NewGuid();
        var partner = CreateUser("Anna");
        partner.TelegramUsername = "anna_k";
        var match = CreateMatch(currentUserId, partner, DateTimeOffset.UtcNow, currentUserSparksBalance: 0);
        _matchRepository.ById = match;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/matches/{match.Id}/unlock");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
        Assert.Null(match.ContactUnlockedAt);
    }

    [Fact(DisplayName = "КОГДА баланса хватает ТОГДА POST /unlock списывает зорки и отдаёт telegramUsername/deepLink")]
    public async Task UnlockContact_spends_sparks_and_returns_telegram_contact()
    {
        var currentUserId = Guid.NewGuid();
        var partner = CreateUser("Anna");
        partner.TelegramUsername = "anna_k";
        var match = CreateMatch(currentUserId, partner, DateTimeOffset.UtcNow, currentUserSparksBalance: 5);
        _matchRepository.ById = match;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/matches/{match.Id}/unlock");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UnlockContactResponse>>(ResponseJsonOptions);

        Assert.Equal("anna_k", body!.Data.TelegramUsername);
        Assert.Equal("https://t.me/anna_k", body.Data.DeepLink);
        Assert.Equal(1, body.Data.SparksSpent);
        Assert.Equal(4, body.Data.SparksBalance);
        Assert.NotNull(match.ContactUnlockedAt);
        Assert.Equal(currentUserId, match.ContactUnlockedByUserId);
    }

    [Fact(DisplayName = "КОГДА контакт уже открыт ТОГДА повторный POST /unlock не списывает зорки повторно")]
    public async Task UnlockContact_is_idempotent_when_already_unlocked()
    {
        var currentUserId = Guid.NewGuid();
        var partner = CreateUser("Anna");
        partner.TelegramUsername = "anna_k";
        var match = CreateMatch(currentUserId, partner, DateTimeOffset.UtcNow, currentUserSparksBalance: 5);
        match.ContactUnlockedAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        match.ContactUnlockedByUserId = currentUserId;
        _matchRepository.ById = match;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/matches/{match.Id}/unlock");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<UnlockContactResponse>>(ResponseJsonOptions);

        Assert.Equal(0, body!.Data.SparksSpent);
        Assert.Equal(5, body.Data.SparksBalance);
    }

    [Fact(DisplayName = "КОГДА мэтч не найден или чужой ТОГДА POST /message-sent-check отвечает 404")]
    public async Task MessageSentCheck_returns_404_when_the_match_does_not_belong_to_the_requesting_user()
    {
        var currentUserId = Guid.NewGuid();
        _matchRepository.ById = CreateMatch(Guid.NewGuid(), CreateUser("Anna"), DateTimeOffset.UtcNow);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/matches/{Guid.NewGuid()}/message-sent-check");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА участник мэтча вызывает message-sent-check ТОГДА отвечает 204 и проставляет MessageSentCheckAt один раз")]
    public async Task MessageSentCheck_sets_the_timestamp_once()
    {
        var currentUserId = Guid.NewGuid();
        var match = CreateMatch(currentUserId, CreateUser("Anna"), DateTimeOffset.UtcNow);
        match.ContactUnlockedAt = DateTimeOffset.UtcNow;
        _matchRepository.ById = match;

        var firstRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/matches/{match.Id}/message-sent-check");
        firstRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));
        var firstResponse = await _client.SendAsync(firstRequest);

        Assert.Equal(HttpStatusCode.NoContent, firstResponse.StatusCode);
        var firstTimestamp = match.MessageSentCheckAt;
        Assert.NotNull(firstTimestamp);

        var secondRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/matches/{match.Id}/message-sent-check");
        secondRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));
        var secondResponse = await _client.SendAsync(secondRequest);

        Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);
        Assert.Equal(firstTimestamp, match.MessageSentCheckAt);
    }

    [Fact(DisplayName = "КОГДА мэтч не найден или чужой ТОГДА POST /archive отвечает 404")]
    public async Task ArchiveMatch_returns_404_when_the_match_does_not_belong_to_the_requesting_user()
    {
        var currentUserId = Guid.NewGuid();
        _matchRepository.ById = CreateMatch(Guid.NewGuid(), CreateUser("Anna"), DateTimeOffset.UtcNow);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/matches/{Guid.NewGuid()}/archive");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА участник мэтча вызывает POST /archive ТОГДА отвечает 204 и мэтч переходит в Archived")]
    public async Task ArchiveMatch_archives_the_match()
    {
        var currentUserId = Guid.NewGuid();
        var match = CreateMatch(currentUserId, CreateUser("Anna"), DateTimeOffset.UtcNow);
        _matchRepository.ById = match;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/matches/{match.Id}/archive");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(MatchStatus.Archived, match.Status);
        Assert.NotNull(match.ArchivedAt);
        Assert.Equal("manual", match.ArchivedReason);
    }

    [Fact(DisplayName = "КОГДА мэтч не найден или чужой ТОГДА DELETE /archive отвечает 404")]
    public async Task UnarchiveMatch_returns_404_when_the_match_does_not_belong_to_the_requesting_user()
    {
        var currentUserId = Guid.NewGuid();
        _matchRepository.ById = CreateMatch(Guid.NewGuid(), CreateUser("Anna"), DateTimeOffset.UtcNow);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/matches/{Guid.NewGuid()}/archive");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(DisplayName = "КОГДА участник мэтча вызывает DELETE /archive ТОГДА отвечает 204 и мэтч возвращается в Active")]
    public async Task UnarchiveMatch_restores_the_match()
    {
        var currentUserId = Guid.NewGuid();
        var match = CreateMatch(currentUserId, CreateUser("Anna"), DateTimeOffset.UtcNow.AddDays(-10));
        match.Status = MatchStatus.Archived;
        match.ArchivedAt = DateTimeOffset.UtcNow.AddDays(-1);
        match.ArchivedReason = "no_activity_7_days";
        _matchRepository.ById = match;

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/matches/{match.Id}/archive");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(currentUserId));

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(MatchStatus.Active, match.Status);
        Assert.Null(match.ArchivedAt);
        Assert.Null(match.ArchivedReason);
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

    private static Match CreateMatch(Guid currentUserId, User other, DateTimeOffset matchedAt, int currentUserSparksBalance = 0)
    {
        var currentUser = new User
        {
            Id = currentUserId, Name = "Me", BirthDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)), Gender = Gender.Male, Locale = "ru",
            SparksBalance = currentUserSparksBalance,
        };
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

        public Match? ById { get; set; }

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

        public Task<Match?> GetByIdForUserAsync(Guid matchId, Guid userId, CancellationToken cancellationToken)
        {
            var found = ById is not null && ById.Id == matchId && (ById.User1Id == userId || ById.User2Id == userId)
                ? ById
                : null;
            return Task.FromResult(found);
        }

        // Тот же объект, что и GetByIdForUserAsync (не настоящий DbContext) — мутации хендлера T-7.3
        // (ContactUnlockedAt/MessageSentCheckAt, User.SparksBalance) видны тесту напрямую через ById без
        // отдельного SaveChangesAsync-состояния.
        public Task<Match?> GetByIdForUserTrackedAsync(Guid matchId, Guid userId, CancellationToken cancellationToken) =>
            GetByIdForUserAsync(matchId, userId, cancellationToken);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<int> ArchiveStaleMatchesAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Не используется в тестах контроллера мэтчей.");
    }

    private sealed class FakeSparkTransactionRepository : ISparkTransactionRepository
    {
        public Task AddAsync(SparkTransaction transaction, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
